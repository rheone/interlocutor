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
/// query/command/void-command handlers, and reports a diagnostic for any query or command request with no
/// matching handler.
/// </summary>
/// <remarks>
/// <para>
/// Scope is intentionally the compiling project only: cross-assembly aggregation of registration modules from
/// referenced assemblies is ticket 12, and open generic handler resolution is ticket 11. A handler or request
/// declared in a referenced assembly (rather than the current compilation) is invisible to this generator -
/// see <see cref="InterlocutorDiagnostics.MissingHandler"/>'s remarks.
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
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analysisResults = context
            .SyntaxProvider.CreateSyntaxProvider(static (node, _) => IsCandidate(node), static (ctx, ct) => Analyze(ctx, ct))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!)
            .Collect();

        context.RegisterSourceOutput(analysisResults, static (spc, results) => Execute(results, spc));
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
    /// Combines every candidate's analysis result into the compilation-wide handler and request lists, reports
    /// the missing-handler diagnostic, and emits the registration module and dispatch wrappers.
    /// </summary>
    private static void Execute(ImmutableArray<TypeAnalysisResult> results, SourceProductionContext context)
    {
        var handlers = results.SelectMany(static r => r.Handlers).ToImmutableArray();
        var requests = results
            .Select(static r => r.Request)
            .Where(static r => r is not null)
            .Select(static r => r!)
            .ToImmutableArray();

        ReportMissingHandlers(handlers, requests, context);

        context.AddSource("InterlocutorGeneratedModuleAttribute.g.cs", RegistrationModuleEmitter.EmitAssemblyAttribute());
        context.AddSource("InterlocutorGeneratedRegistrations.g.cs", RegistrationModuleEmitter.Emit(handlers));
        context.AddSource("InterlocutorGeneratedDispatch.g.cs", DispatchWrapperEmitter.Emit(handlers));
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.MissingHandler"/> for every request with no matching handler
    /// in <paramref name="handlers"/>. A query or command request matches a handler with the same request and
    /// response type; a void command matches any void-command handler for the same request type regardless of
    /// the handler's chosen result type (see <see cref="RequestKind.VoidCommand"/>).
    /// </summary>
    private static void ReportMissingHandlers(
        ImmutableArray<HandlerModel> handlers,
        ImmutableArray<RequestModel> requests,
        SourceProductionContext context
    )
    {
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
}
