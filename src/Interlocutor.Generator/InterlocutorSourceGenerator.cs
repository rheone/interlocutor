namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Diagnostics;
using Interlocutor.Generator.Emission;
using Interlocutor.Generator.Models;
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

    /// <summary>
    /// The tracking name given to the <see cref="GenericConstraintSnapshot"/> pipeline step (ticket 53), so a
    /// test driven with <c>trackIncrementalGeneratorSteps: true</c> can assert this specific step's cache
    /// behavior directly - see <see cref="GenericConstraintSnapshot"/>'s remarks.
    /// </summary>
    public const string GenericConstraintSnapshotTrackingName = "GenericConstraintSnapshot";

    /// <summary>
    /// The tracking name given to the value provider combining every input <see cref="Execute"/> depends on,
    /// immediately before <c>RegisterSourceOutput</c>
    /// (ticket 53) - so a test can assert the pipeline's final, fully-combined step (not just one of its
    /// individual inputs) reports <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Cached"/> or
    /// <see cref="Microsoft.CodeAnalysis.IncrementalStepRunReason.Unchanged"/>, proving real end-to-end
    /// incrementality rather than just one well-behaved step among others.
    /// </summary>
    public const string FinalPipelineTrackingName = "FinalPipeline";

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
        // changes no public API surface and so changes nothing ReferencedModuleDiscovery reads.
        var compilationInfo = context
            .CompilationProvider.Select(
                static (compilation, ct) =>
                    new CompilationInfo(
                        IdentifierSanitizer.ToNamespaceSegment(compilation.AssemblyName ?? "GeneratedAssembly"),
                        ReferencedModuleDiscovery.Discover(compilation, ct),
                        compilation.GetTypeByMetadataName(WellKnownTypeNames.Query) is not null
                    )
            )
            .WithTrackingName(CompilationInfoTrackingName);

        var openGenericHandlersOnly = analysisResults.Select(
            static (results, _) => results.SelectMany(static r => r.OpenGenericHandlers).ToImmutableArray()
        );

        // The only place left in the whole pipeline that still needs the raw Compilation (ticket 53) - see
        // GenericConstraintSnapshot's remarks. This step's own transform function re-runs on every rebuild
        // regardless (CompilationProvider is never equal to itself across runs, by design), but the *value* it
        // produces has real, element-wise structural equality, so a rebuild whose open generic handlers and
        // generic usages are unchanged produces a snapshot that compares equal to the previous run's - letting
        // Execute, and therefore RegisterSourceOutput as a whole, skip re-emitting source entirely.
        var genericConstraintSnapshot = context
            .CompilationProvider.Combine(openGenericHandlersOnly)
            .Combine(genericUsages)
            .Select(static (data, _) => GenericConstraintSnapshotBuilder.Build(data.Left.Left, data.Left.Right, data.Right))
            .WithTrackingName(GenericConstraintSnapshotTrackingName);

        var combined = analysisResults
            .Combine(genericUsages)
            .Combine(compilationInfo)
            .Combine(genericConstraintSnapshot)
            .WithTrackingName(FinalPipelineTrackingName);

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
    /// Combines every candidate's analysis result, generic usages, compilation-level info, and the open
    /// generic constraint snapshot into the compilation-wide handler and request lists, closes every open
    /// generic handler over its discovered concrete usages (ticket 11), reports the missing-handler diagnostic
    /// (now considering upstream referenced modules too, per ticket 12), and emits the registration module and
    /// dispatch wrappers.
    /// </summary>
    private static void Execute(
        ImmutableArray<TypeAnalysisResult> results,
        ImmutableArray<ClosedGenericRequestUsageModel> genericUsages,
        CompilationInfo compilationInfo,
        GenericConstraintSnapshot genericConstraintSnapshot,
        SourceProductionContext context
    )
    {
        if (!compilationInfo.HasAbstractionsReference)
        {
            // This compilation never references Interlocutor.Abstractions at all (Interlocutor.CommonKernel's
            // own build, wiring this assembly in purely for OutcomeMemberGenerator - ticket 05) - every emitter
            // below unconditionally names Abstractions/DependencyInjection.Abstractions types, so there is
            // nothing safe to emit here.
            return;
        }

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
            genericConstraintSnapshot
        );
        foreach (var diagnostic in openGenericResolution.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        handlers = handlers.AddRange(openGenericResolution.ClosedHandlers);

        HandlerDiagnosticAnalyzer.ReportMissingHandlers(handlers, compilationInfo.ExternalModules, requests, context);
        HandlerDiagnosticAnalyzer.ReportUnresolvedGenericRequestUsages(
            handlers,
            compilationInfo.ExternalModules,
            genericUsages,
            openGenericResolution.AttemptedUsageDisplayNames,
            context
        );
        HandlerDiagnosticAnalyzer.ReportDuplicateHandlers(handlers, context);
        HandlerDiagnosticAnalyzer.ReportCatchAllNotificationHandlers(handlers, context);

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
}
