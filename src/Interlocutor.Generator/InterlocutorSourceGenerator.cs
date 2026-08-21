namespace Interlocutor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Discovers Interlocutor query, command, void-command, and notification handlers in the current compilation
/// at build time (ADR-0008), emits a container-neutral registration module, emits closed dispatch wrappers for
/// query/command/void-command handlers, emits a reflection-free type switch dispatching a runtime-typed
/// request across all of them (ticket 13), and reports a diagnostic for any query or command request with no
/// matching handler.
/// </summary>
/// <remarks>
/// <para>
/// Open generic query/command handlers are closed over every concrete usage found anywhere in the
/// compilation (ticket 11) - see <see cref="GenericUsageAnalyzer"/> and <see cref="OpenGenericHandlerResolver"/>.
/// </para>
/// <para>
/// A referenced assembly's own generated module and handlers are discovered too (ticket 12), via
/// <see cref="ReferencedModuleDiscovery"/> - purely from compile-time assembly metadata
/// (<c>InterlocutorGeneratedModuleAttribute</c>/<c>InterlocutorGeneratedHandlerAttribute</c>), never by
/// loading or reflecting over the referenced assembly at run time. This compilation's own module aggregates
/// every discovered upstream module's registrations, and the missing-handler diagnostic treats an upstream
/// handler as satisfying a locally-declared request. A request or handler declared only in a *sibling*
/// assembly that neither this compilation nor its transitive references reach remains invisible, as does any
/// handler in an assembly loaded only at run time (the province of the explicitly opted-in runtime scanning
/// fallback) - see <see cref="InterlocutorDiagnostics.MissingHandler"/>'s remarks.
/// </para>
/// <para>
/// Every type implementing one of the four handler contracts or three request markers is found via Roslyn's
/// semantic model (<see cref="HandlerTypeAnalyzer"/>) - never by matching interface names as source text -
/// exactly replicating <c>Interlocutor.DependencyInjection.Abstractions.HandlerDiscovery</c>'s reflection-based
/// discovery semantics at compile time instead of runtime.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class InterlocutorSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The tracking name given to the <see cref="CompilationInfo"/> pipeline step, so a test driven with
    /// <c>trackIncrementalGeneratorSteps: true</c> can assert this specific step's cache behavior directly
    /// (see <see cref="CompilationInfo"/>'s remarks on what this step's caching does and does not cover).
    /// Public rather than internal so <c>Interlocutor.Generator.Tests</c> can reference it without this
    /// project taking on an <c>InternalsVisibleTo</c> it otherwise has no need for.
    /// </summary>
    public const string CompilationInfoTrackingName = "CompilationInfo";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analysisResults = context
            .SyntaxProvider.CreateSyntaxProvider(static (node, _) => IsCandidate(node), static (ctx, ct) => Analyze(ctx, ct))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!)
            .Collect();

        // A second, independent syntax pass (ticket 11): every fully closed generic type usage anywhere in the
        // compilation, matched against discovered open generic handlers' request types in Execute once both
        // sides are known - see GenericUsageAnalyzer's remarks.
        var genericUsages = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => GenericUsageAnalyzer.IsCandidate(node),
                static (ctx, _) => GenericUsageAnalyzer.Analyze(ctx)
            )
            .Where(static usage => usage is not null)
            .Select(static (usage, _) => usage!)
            .Collect();

        // Compilation-level (rather than per-syntax-node) because both facts - this compilation's own
        // assembly name and every referenced assembly's discovered module - are properties of the whole
        // reference graph, not of any one candidate type declaration. CompilationInfo has real, element-wise
        // structural equality (see its own remarks), so this step itself is cached whenever a rebuild's
        // reference graph is unchanged - for example, editing a method body inside a referenced library that
        // changes no public API surface and so changes nothing ReferencedModuleDiscovery reads. That said, the
        // combined pipeline below also threads the raw Compilation through unconditionally (needed by
        // OpenGenericHandlerResolver.Resolve's semantic-model lookups), and Compilation is never equal across
        // runs - so RegisterSourceOutput as a whole still re-executes on every edit today regardless of this
        // step's own caching. Making the overall pipeline skip re-emission end to end would mean reworking how
        // open generic resolution gets what it needs from the compilation, which is outside this ticket's scope.
        var compilationInfo = context
            .CompilationProvider.Select(
                static (compilation, ct) =>
                    new CompilationInfo(
                        IdentifierSanitizer.ToNamespaceSegment(compilation.AssemblyName ?? "GeneratedAssembly"),
                        ReferencedModuleDiscovery.Discover(compilation, ct)
                    )
            )
            .WithTrackingName(CompilationInfoTrackingName);

        var combined = analysisResults.Combine(genericUsages).Combine(context.CompilationProvider).Combine(compilationInfo);

        context.RegisterSourceOutput(
            combined,
            static (spc, data) => Execute(data.Left.Left.Left, data.Left.Left.Right, data.Left.Right, data.Right, spc)
        );
    }

    /// <summary>
    /// Fast syntactic filter: any non-abstract class, struct, or record declaration with a base list is a
    /// candidate for semantic analysis. Interfaces, abstract types, and types with no base list (which
    /// therefore cannot implement any Interlocutor contract) are excluded before the semantic model runs.
    /// </summary>
    private static bool IsCandidate(SyntaxNode node) =>
        node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 } typeDeclaration
        && typeDeclaration.Kind() is not SyntaxKind.InterfaceDeclaration
        && !typeDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);

    /// <summary>Runs full semantic analysis on one syntactically-qualifying candidate.</summary>
    private static TypeAnalysisResult? Analyze(GeneratorSyntaxContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, ct) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var location = typeDeclaration.Identifier.GetLocation();
        var result = HandlerTypeAnalyzer.Analyze(symbol, location, context.SemanticModel.Compilation);
        return result == TypeAnalysisResult.Empty ? null : result;
    }

    /// <summary>
    /// Combines every candidate's analysis result, generic usages, the compilation, and compilation-level info
    /// into the compilation-wide handler and request lists, closes every open generic handler over its
    /// discovered concrete usages (ticket 11), reports the missing-handler diagnostic (now considering upstream
    /// referenced modules too, per ticket 12), and emits the registration module and dispatch wrappers.
    /// </summary>
    private static void Execute(
        ImmutableArray<TypeAnalysisResult> results,
        ImmutableArray<ClosedGenericRequestUsageModel> genericUsages,
        Compilation compilation,
        CompilationInfo compilationInfo,
        SourceProductionContext context
    )
    {
        var handlers = results.SelectMany(static r => r.Handlers).ToImmutableArray();
        var openGenericHandlers = results.SelectMany(static r => r.OpenGenericHandlers).ToImmutableArray();
        var requests = results
            .Select(static r => r.Request)
            .Where(static r => r is not null)
            .Select(static r => r!)
            .ToImmutableArray();

        var openGenericResolution = OpenGenericHandlerResolver.Resolve(
            handlers,
            openGenericHandlers,
            genericUsages,
            compilation
        );
        foreach (var diagnostic in openGenericResolution.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        handlers = handlers.AddRange(openGenericResolution.ClosedHandlers);

        ReportMissingHandlers(handlers, compilationInfo.ExternalModules, requests, context);
        ReportDuplicateHandlers(handlers, context);
        ReportCatchAllNotificationHandlers(handlers, context);

        var notificationHandlers = handlers.Where(static h => h.Kind == HandlerKind.Notification).ToImmutableArray();
        var order = NotificationHandlerOrderPlanner.Compute(notificationHandlers);
        foreach (var diagnostic in order.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        context.AddSource(
            "InterlocutorGeneratedModuleAttribute.g.cs",
            RegistrationModuleEmitter.EmitAssemblyAttributes(handlers, compilationInfo.AssemblyNamespaceSegment)
        );
        context.AddSource(
            "InterlocutorGeneratedRegistrations.g.cs",
            RegistrationModuleEmitter.Emit(handlers, compilationInfo.AssemblyNamespaceSegment, compilationInfo.ExternalModules)
        );
        context.AddSource("InterlocutorGeneratedDispatch.g.cs", DispatchWrapperEmitter.Emit(handlers));
        context.AddSource("InterlocutorGeneratedDispatchSwitch.g.cs", RequestDispatchSwitchEmitter.Emit(handlers, requests));
        context.AddSource("InterlocutorGeneratedNotificationOrder.g.cs", NotificationHandlerOrderEmitter.Emit(order.Ranks));
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.CatchAllNotificationHandler"/> for every notification handler
    /// implementing <c>INotificationHandler&lt;INotification&gt;</c> - subscribing to the base marker interface
    /// itself, and therefore to every notification published anywhere in the process.
    /// </summary>
    private static void ReportCatchAllNotificationHandlers(
        ImmutableArray<HandlerModel> handlers,
        SourceProductionContext context
    )
    {
        foreach (var handler in handlers)
        {
            if (
                handler.Kind == HandlerKind.Notification
                && handler.RequestTypeName == "global::" + WellKnownTypeNames.Notification
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.CatchAllNotificationHandler,
                        handler.Location,
                        handler.HandlerTypeName
                    )
                );
            }
        }
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.MissingHandler"/> for every request with no matching handler
    /// among <paramref name="handlers"/> or any upstream <paramref name="externalModules"/>'s own handlers. A
    /// query or command request matches a handler with the same request and response type; a void command
    /// matches any void-command handler for the same request type regardless of the handler's chosen result
    /// type (see <see cref="RequestKind.VoidCommand"/>).
    /// </summary>
    private static void ReportMissingHandlers(
        ImmutableArray<HandlerModel> handlers,
        ImmutableArray<ExternalModuleInfo> externalModules,
        ImmutableArray<RequestModel> requests,
        SourceProductionContext context
    )
    {
        var externalHandlers = externalModules.SelectMany(static m => m.Handlers);

        var queryHandlerRequestTypes = new HashSet<(string, string?)>(
            handlers.Where(static h => h.Kind == HandlerKind.Query).Select(static h => (h.RequestTypeName, h.ResponseTypeName))
        );
        var commandHandlerRequestTypes = new HashSet<(string, string?)>(
            handlers
                .Where(static h => h.Kind == HandlerKind.Command)
                .Select(static h => (h.RequestTypeName, h.ResponseTypeName))
        );
        var voidCommandHandlerRequestTypes = new HashSet<string>(
            handlers.Where(static h => h.Kind == HandlerKind.VoidCommand).Select(static h => h.RequestTypeName)
        );

        foreach (var externalHandler in externalHandlers)
        {
            switch (externalHandler.Kind)
            {
                case HandlerKind.Query:
                    queryHandlerRequestTypes.Add((externalHandler.RequestTypeName, externalHandler.ResponseTypeName));
                    break;
                case HandlerKind.Command:
                    commandHandlerRequestTypes.Add((externalHandler.RequestTypeName, externalHandler.ResponseTypeName));
                    break;
                case HandlerKind.VoidCommand:
                    voidCommandHandlerRequestTypes.Add(externalHandler.RequestTypeName);
                    break;
                case HandlerKind.Notification:
                default:
                    // Never emitted for external handlers - see RegistrationModuleEmitter.EmitAssemblyAttributes.
                    break;
            }
        }

        foreach (var request in requests)
        {
            var hasHandler = request.Kind switch
            {
                RequestKind.Query => queryHandlerRequestTypes.Contains((request.RequestTypeName, request.ResponseTypeName)),
                RequestKind.Command => commandHandlerRequestTypes.Contains((request.RequestTypeName, request.ResponseTypeName)),
                RequestKind.VoidCommand => voidCommandHandlerRequestTypes.Contains(request.RequestTypeName),
                _ => true,
            };

            if (!hasHandler)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(InterlocutorDiagnostics.MissingHandler, request.Location, request.RequestTypeName)
                );
            }
        }
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.DuplicateHandler"/> whenever more than one query or command
    /// handler is found for the same closed request/response pair. A notification's zero-or-many arity (see
    /// <see cref="HandlerKind.Notification"/>) and a void command's caller-chosen result type (see
    /// <see cref="RequestKind.VoidCommand"/>) both make "more than one handler" the expected, valid shape for
    /// those kinds, so only <see cref="HandlerKind.Query"/> and <see cref="HandlerKind.Command"/> are checked.
    /// </summary>
    private static void ReportDuplicateHandlers(ImmutableArray<HandlerModel> handlers, SourceProductionContext context)
    {
        var duplicateGroups = handlers
            .Where(static h => h.Kind is HandlerKind.Query or HandlerKind.Command)
            .GroupBy(static h => (h.RequestTypeName, h.ResponseTypeName))
            .Where(static group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var duplicateHandlers = group.ToImmutableArray();
            var handlerNames = string.Join(", ", duplicateHandlers.Select(static h => h.HandlerTypeName));

            foreach (var handler in duplicateHandlers)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.DuplicateHandler,
                        handler.Location,
                        handler.RequestTypeName,
                        handlerNames
                    )
                );
            }
        }
    }
}
