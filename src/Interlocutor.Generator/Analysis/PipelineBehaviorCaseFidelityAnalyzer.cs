namespace Interlocutor.Generator.Analysis;

using System.Collections.Immutable;
using System.Linq;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Reports <see cref="InterlocutorDiagnostics.PipelineBehaviorCaseFidelityMismatch"/> for a command/query
/// request implementing one of the four marker-interface-gated pipeline behaviors' marker interfaces
/// (<c>IRequiresPolicyRequest</c>, <c>ICancellableRequest</c>, <c>IRateLimitedRequest</c>,
/// <c>ITimeoutRequest</c>) whose declared response type has no case the corresponding behavior can construct.
/// </summary>
/// <remarks>
/// <para>
/// Scope, per ADR-0023's "Analyzer scope" section: only these four marker-interface-gated behaviors -
/// <c>AuthorizationBehavior</c>, <c>CancellationBehavior</c>, <c>RateLimitedBehavior</c>,
/// <c>TimeoutBehavior</c> - are covered. <c>ValidationBehavior</c>/<c>FluentValidationErrorsBehavior</c> are
/// gated by whether a <c>FluentValidation.IValidator&lt;TRequest&gt;</c> is actually registered in the built
/// container - a DI-registration-time fact invisible to a compile-time analyzer working only from a type's
/// declared shape - so those two stay out of scope for this diagnostic; see
/// <see cref="ValidationShapeMismatchAnalyzer"/> for the closest analogous, already-shipped check those two
/// behaviors do get build-time coverage from.
/// </para>
/// <para>
/// Purely symbol-based, mirroring <see cref="ValidationShapeMismatchAnalyzer"/>: everything this check depends
/// on - the request's marker interfaces, its declared response type via <c>ICommand&lt;TResponse&gt;</c>/
/// <c>IQuery&lt;TResponse&gt;</c>, and that response type's own case types - is visible on the compilation's
/// symbol graph directly, with no DI/runtime registration information needed. The runtime construction
/// mechanism (<c>UnionCaseFactory.FindCaseConstructor</c>) requires the response's declared case type to be
/// exactly the behavior's concrete case type (<c>Forbidden</c>, <c>Cancelled</c>, <c>RateLimited</c>,
/// <c>Timeout</c>) - not merely a type implementing the same failure-case marker interface that concrete type
/// happens to implement - so this analyzer checks for that same exact case type, never a looser "implements"
/// match.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PipelineBehaviorCaseFidelityAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [InterlocutorDiagnostics.PipelineBehaviorCaseFidelityMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var compilation = compilationStartContext.Compilation;
            var commandMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Command);
            var queryMarker = compilation.GetTypeByMetadataName(WellKnownTypeNames.Query);
            var nonGenericOutcome = compilation.GetTypeByMetadataName(WellKnownTypeNames.ConcreteNonGenericOutcome);

            if (commandMarker is null && queryMarker is null)
            {
                // Interlocutor.Abstractions isn't referenced - no ICommand<>/IQuery<> to correlate against.
                return;
            }

            var behaviors = BuildBehaviorRequirements(compilation);
            if (behaviors.IsEmpty)
            {
                // Interlocutor.CommonKernel isn't referenced (or none of the four markers are), so none of the
                // four marker interfaces or case types this diagnostic checks even resolve.
                return;
            }

            compilationStartContext.RegisterSymbolAction(
                symbolContext => Analyze(symbolContext, commandMarker, queryMarker, nonGenericOutcome, behaviors),
                SymbolKind.NamedType
            );
        });
    }

    /// <summary>
    /// Resolves each of the four marker interface/required case type pairs this analyzer checks, skipping any
    /// pair whose marker or case type symbol doesn't resolve in <paramref name="compilation"/> (a partial
    /// reference to <c>Interlocutor.Abstractions</c>/<c>Interlocutor.CommonKernel</c>, or an older/newer
    /// version missing one of these types).
    /// </summary>
    private static ImmutableArray<BehaviorRequirement> BuildBehaviorRequirements(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<BehaviorRequirement>();

        AddIfResolvable(
            compilation,
            builder,
            WellKnownTypeNames.RequiresPolicyRequestMarker,
            WellKnownTypeNames.ForbiddenCase,
            "AuthorizationBehavior"
        );
        AddIfResolvable(
            compilation,
            builder,
            WellKnownTypeNames.CancellableRequestMarker,
            WellKnownTypeNames.CancelledCase,
            "CancellationBehavior"
        );
        AddIfResolvable(
            compilation,
            builder,
            WellKnownTypeNames.RateLimitedRequestMarker,
            WellKnownTypeNames.RateLimitedCase,
            "RateLimitedBehavior"
        );
        AddIfResolvable(
            compilation,
            builder,
            WellKnownTypeNames.TimeoutRequestMarker,
            WellKnownTypeNames.TimeoutCase,
            "TimeoutBehavior"
        );

        return builder.ToImmutable();
    }

    /// <summary>Resolves one marker/case pair and appends it to <paramref name="builder"/> if both resolve.</summary>
    private static void AddIfResolvable(
        Compilation compilation,
        ImmutableArray<BehaviorRequirement>.Builder builder,
        string markerMetadataName,
        string caseMetadataName,
        string behaviorName
    )
    {
        var marker = compilation.GetTypeByMetadataName(markerMetadataName);
        var caseType = compilation.GetTypeByMetadataName(caseMetadataName);
        if (marker is not null && caseType is not null)
        {
            builder.Add(new BehaviorRequirement(marker, caseType, behaviorName));
        }
    }

    /// <summary>
    /// Checks one candidate named type: if it declares a response type via <c>ICommand&lt;TResponse&gt;</c>/
    /// <c>IQuery&lt;TResponse&gt;</c>, reports <see cref="InterlocutorDiagnostics.PipelineBehaviorCaseFidelityMismatch"/>
    /// once for every <paramref name="behaviors"/> entry whose marker interface the candidate implements but
    /// whose required case type the response's declared case set doesn't include.
    /// </summary>
    private static void Analyze(
        SymbolAnalysisContext context,
        INamedTypeSymbol? commandMarker,
        INamedTypeSymbol? queryMarker,
        INamedTypeSymbol? nonGenericOutcome,
        ImmutableArray<BehaviorRequirement> behaviors
    )
    {
        var requestType = (INamedTypeSymbol)context.Symbol;
        var responseType = FindDeclaredResponseType(requestType, commandMarker, queryMarker);
        if (responseType is null)
        {
            // Not an ICommand<TResponse>/IQuery<TResponse> - out of scope for this rule (bare ICommand has no
            // response type to check case fidelity against either).
            return;
        }

        if (!TryGetCaseTypes(responseType, nonGenericOutcome, out var caseTypes))
        {
            // The response's case set can't be determined statically (a fully custom, non-generic IUnion
            // implementer) - skip rather than risk a false positive.
            return;
        }

        foreach (var behavior in behaviors)
        {
            if (!requestType.AllInterfaces.Contains(behavior.MarkerInterface, SymbolEqualityComparer.Default))
            {
                continue;
            }

            if (caseTypes.Any(caseType => SymbolEqualityComparer.Default.Equals(caseType, behavior.CaseType)))
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    InterlocutorDiagnostics.PipelineBehaviorCaseFidelityMismatch,
                    requestType.Locations.FirstOrDefault() ?? Location.None,
                    requestType.ToDisplayString(),
                    behavior.MarkerInterface.Name,
                    responseType.ToDisplayString(),
                    behavior.CaseType.Name,
                    behavior.BehaviorName
                )
            );
        }
    }

    /// <summary>
    /// Finds the response type <paramref name="requestType"/> declares via
    /// <c>ICommand&lt;TResponse&gt;</c>/<c>IQuery&lt;TResponse&gt;</c>, if it implements either.
    /// </summary>
    /// <param name="requestType">The candidate request type.</param>
    /// <param name="commandMarker">The resolved <c>ICommand&lt;TResponse&gt;</c> open generic symbol, if referenced.</param>
    /// <param name="queryMarker">The resolved <c>IQuery&lt;TResponse&gt;</c> open generic symbol, if referenced.</param>
    /// <returns>The declared response type, or <see langword="null"/> if <paramref name="requestType"/> is neither a command nor a query.</returns>
    private static ITypeSymbol? FindDeclaredResponseType(
        INamedTypeSymbol requestType,
        INamedTypeSymbol? commandMarker,
        INamedTypeSymbol? queryMarker
    )
    {
        foreach (var candidateInterface in requestType.AllInterfaces)
        {
            if (!candidateInterface.IsGenericType)
            {
                continue;
            }

            var openDefinition = candidateInterface.OriginalDefinition;
            if (
                SymbolEqualityComparer.Default.Equals(openDefinition, commandMarker)
                || SymbolEqualityComparer.Default.Equals(openDefinition, queryMarker)
            )
            {
                return candidateInterface.TypeArguments[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the response type's case types - the type arguments of a closed generic union, or the empty
    /// set for the shipped, case-less non-generic <c>Outcome</c>. Mirrors
    /// <c>ValidationShapeMismatchAnalyzer.TryGetCaseTypes</c>.
    /// </summary>
    /// <param name="responseType">The request's declared response type.</param>
    /// <param name="nonGenericOutcome">The resolved, shipped non-generic <c>Outcome</c> symbol, if referenced.</param>
    /// <param name="caseTypes">The resolved case types, empty for the non-generic <c>Outcome</c>.</param>
    /// <returns><see langword="true"/> if the case set could be determined; <see langword="false"/> for an unrecognized non-generic union.</returns>
    private static bool TryGetCaseTypes(
        ITypeSymbol responseType,
        INamedTypeSymbol? nonGenericOutcome,
        out ImmutableArray<ITypeSymbol> caseTypes
    )
    {
        if (responseType is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } namedResponse)
        {
            caseTypes = namedResponse.TypeArguments;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(responseType.OriginalDefinition, nonGenericOutcome))
        {
            // Case-less acknowledgment - no case is ever present, since there are no cases at all.
            caseTypes = ImmutableArray<ITypeSymbol>.Empty;
            return true;
        }

        caseTypes = ImmutableArray<ITypeSymbol>.Empty;
        return false;
    }

    /// <summary>One marker-interface-gated behavior's requirement: the marker it's gated by, and the exact case type it constructs.</summary>
    /// <param name="MarkerInterface">The marker interface symbol that opts a request into this behavior.</param>
    /// <param name="CaseType">The exact case type this behavior constructs when it short-circuits.</param>
    /// <param name="BehaviorName">The behavior's own simple name, for the diagnostic message.</param>
    private sealed record BehaviorRequirement(INamedTypeSymbol MarkerInterface, INamedTypeSymbol CaseType, string BehaviorName);
}
