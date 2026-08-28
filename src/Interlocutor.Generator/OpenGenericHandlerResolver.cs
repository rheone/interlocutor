namespace Interlocutor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

/// <summary>
/// Closes every discovered open generic query/command handler over every concrete request type it matches
/// (ticket 11), applying - in order - the "closed beats open" precedence rule, generic-parameter constraint
/// checking, and the <c>HandlerOverrideAttribute</c> escape hatch for a genuine open-vs-open ambiguity.
/// </summary>
/// <remarks>
/// <para>
/// There is exactly one selection rule between two open generic handlers that both match the same closed
/// request: an explicit <c>HandlerOverrideAttribute</c> naming the other. No ranking by inheritance distance,
/// namespace, or assembly location is ever considered - an unresolved ambiguity is always
/// <see cref="InterlocutorDiagnostics.OpenGenericHandlerAmbiguity"/>, naming both handlers, never an arbitrary
/// runtime choice.
/// </para>
/// <para>
/// Every closed <see cref="HandlerModel"/> this produces is built by whole-word substitution of the open
/// handler's own type parameter names inside its already-captured display-name templates (see
/// <see cref="OpenGenericHandlerModel"/>'s remarks) - the result flows into the exact same emitters
/// (<see cref="RegistrationModuleEmitter"/>, <see cref="DispatchWrapperEmitter"/>,
/// <see cref="RequestDispatchSwitchEmitter"/>) that handle every other closed handler, so a closed-over-generic
/// handler is indistinguishable from a hand-written closed handler once resolution completes.
/// </para>
/// <para>
/// This resolution is deliberately same-compilation-only (ticket 15): <see cref="Resolve"/>'s
/// <c>openHandlers</c> and <c>usages</c> parameters are both discovered purely from the current compilation's
/// own syntax trees, never from a referenced assembly's metadata the way
/// <see cref="ReferencedModuleDiscovery"/> aggregates closed handlers. Spanning assemblies would require
/// serializing <see cref="OpenGenericHandlerModel.Constraints"/> - checked today against live
/// <c>Compilation</c> symbols via <see cref="GenericConstraintSnapshot"/> - into assembly-level metadata
/// faithfully enough to re-check constraints in a downstream compilation that never sees the declaring
/// library's syntax; considered and deliberately not taken, as a materially larger, higher-risk undertaking
/// than reusing an established, cheaper mechanism. Instead, a closed generic request usage that resolves to
/// no handler anywhere in the reference graph - not locally, not in a referenced assembly's generated module -
/// is caught at build time by <see cref="InterlocutorDiagnostics.UnresolvedGenericRequestUsage"/> (see
/// <see cref="HandlerDiagnosticAnalyzer.ReportUnresolvedGenericRequestUsages"/>), replacing what was
/// previously a silent gap only surfaced by a runtime <c>HandlerNotRegisteredException</c> at first dispatch.
/// </para>
/// </remarks>
internal static class OpenGenericHandlerResolver
{
    /// <summary>
    /// Resolves every open generic handler against every discovered closed generic usage.
    /// </summary>
    /// <param name="closedHandlers">Every already-closed query/command handler discovered in the compilation.</param>
    /// <param name="openHandlers">Every open generic query/command handler discovered in the compilation.</param>
    /// <param name="usages">Every fully closed generic type usage found anywhere in the compilation.</param>
    /// <param name="snapshot">The pre-resolved constraint/argument facts to check against - see <see cref="GenericConstraintSnapshot"/>.</param>
    /// <returns>The closed handlers produced by successfully resolved matches, plus any diagnostics.</returns>
    public static Result Resolve(
        ImmutableArray<HandlerModel> closedHandlers,
        ImmutableArray<OpenGenericHandlerModel> openHandlers,
        ImmutableArray<ClosedGenericRequestUsageModel> usages,
        GenericConstraintSnapshot snapshot
    )
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        ReportInvalidOverrideTargets(openHandlers, closedHandlers, diagnostics);

        var closedRequestTypeNames = new HashSet<string>(
            closedHandlers
                .Where(static h => h.Kind is HandlerKind.Query or HandlerKind.Command)
                .Select(static h => h.RequestTypeName),
            System.StringComparer.Ordinal
        );

        // Multiple usages can instantiate the exact same closed request type (e.g. GenericQuery<Foo> used in
        // two different places) - only one closing is needed per distinct closed request.
        var dedupedUsages = usages.GroupBy(static u => u.ClosedRequestDisplayName).Select(static g => g.First());

        var closedResults = ImmutableArray.CreateBuilder<HandlerModel>();
        var reportedAmbiguities = new HashSet<string>(System.StringComparer.Ordinal);

        // Every usage at least one local open generic handler's request definition key matched, whether
        // resolution succeeded, was left ambiguous, or failed a constraint check - tracked so
        // HandlerDiagnosticAnalyzer.ReportUnresolvedGenericRequestUsages (ticket 15) never double-reports a
        // usage OpenGenericHandlerAmbiguity already covers.
        var attemptedUsageDisplayNames = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var group in openHandlers.GroupBy(static h => h.RequestDefinitionKey))
        {
            var handlersInGroup = group.ToImmutableArray();
            foreach (var usage in dedupedUsages.Where(u => u.RequestDefinitionKey == group.Key))
            {
                attemptedUsageDisplayNames.Add(usage.ClosedRequestDisplayName);

                if (closedRequestTypeNames.Contains(usage.ClosedRequestDisplayName))
                {
                    // A closed handler for this exact request already exists - it wins outright, and no
                    // open-vs-open ambiguity is even considered for this request.
                    continue;
                }

                ResolveUsage(handlersInGroup, usage, snapshot, closedResults, diagnostics, reportedAmbiguities);
            }
        }

        return new Result(
            closedResults.ToImmutable(),
            diagnostics.ToImmutable(),
            attemptedUsageDisplayNames.ToImmutableArray()
        );
    }

    /// <summary>Resolves every open generic handler in <paramref name="handlersInGroup"/> against one discovered closed usage.</summary>
    private static void ResolveUsage(
        ImmutableArray<OpenGenericHandlerModel> handlersInGroup,
        ClosedGenericRequestUsageModel usage,
        GenericConstraintSnapshot snapshot,
        ImmutableArray<HandlerModel>.Builder closedResults,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        HashSet<string> reportedAmbiguities
    )
    {
        var matches = ImmutableArray.CreateBuilder<MatchedHandler>();
        foreach (var handler in handlersInGroup)
        {
            if (handler.RequestArgumentParameterNames.Length != usage.TypeArgumentDisplayNames.Length)
            {
                continue;
            }

            var substitutions = handler
                .RequestArgumentParameterNames.Zip(usage.TypeArgumentDisplayNames, static (name, arg) => (name, arg))
                .ToImmutableDictionary(static t => t.name, static t => t.arg, System.StringComparer.Ordinal);

            if (SatisfiesEveryConstraint(handler, usage, snapshot))
            {
                matches.Add(new MatchedHandler(handler, substitutions));
            }
        }

        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            closedResults.Add(CloseHandler(matches[0], usage));
            return;
        }

        var winner = matches.FirstOrDefault(candidate =>
            matches.All(other =>
                Equals(other.Handler, candidate.Handler)
                || candidate.Handler.OverrideTargetKeys.Contains(other.Handler.HandlerDefinitionKey)
            )
        );

        if (winner is not null)
        {
            closedResults.Add(CloseHandler(winner, usage));
            return;
        }

        var sortedKeys = matches
            .Select(static m => m.Handler.HandlerDefinitionKey)
            .OrderBy(static k => k, System.StringComparer.Ordinal);
        var ambiguityKey = usage.ClosedRequestDisplayName + "|" + string.Join(",", sortedKeys);
        if (!reportedAmbiguities.Add(ambiguityKey))
        {
            return;
        }

        var handlerNames = string.Join(
            ", ",
            matches
                .Select(static m => m.Handler)
                .OrderBy(static h => h.HandlerDisplayName, System.StringComparer.Ordinal)
                .Select(static h => h.HandlerDisplayName)
        );
        foreach (var match in matches)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    InterlocutorDiagnostics.OpenGenericHandlerAmbiguity,
                    match.Handler.Location,
                    usage.ClosedRequestDisplayName,
                    handlerNames
                )
            );
        }
    }

    /// <summary>Whether every constraint on <paramref name="handler"/>'s own type parameters is satisfied for <paramref name="usage"/>'s type arguments.</summary>
    private static bool SatisfiesEveryConstraint(
        OpenGenericHandlerModel handler,
        ClosedGenericRequestUsageModel usage,
        GenericConstraintSnapshot snapshot
    )
    {
        foreach (var constraint in handler.Constraints)
        {
            var position = handler.RequestArgumentParameterNames.IndexOf(constraint.ParameterName);
            var argumentMetadataName = position >= 0 ? usage.TypeArgumentMetadataNames[position] : null;
            if (!GenericConstraintChecker.Satisfies(constraint, argumentMetadataName, snapshot))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Builds the closed <see cref="HandlerModel"/> for one resolved match.</summary>
    private static HandlerModel CloseHandler(MatchedHandler match, ClosedGenericRequestUsageModel usage) =>
        new(
            match.Handler.Kind,
            Substitute(match.Handler.HandlerDisplayName, match.Substitutions),
            usage.ClosedRequestDisplayName,
            Substitute(match.Handler.ResponseDisplayTemplate, match.Substitutions),
            Substitute(match.Handler.ServiceInterfaceDisplayTemplate, match.Substitutions),
            match.Handler.Location
        );

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.HandlerOverrideInvalidTarget"/> for every
    /// <c>HandlerOverrideAttribute</c> target that does not name a discovered handler (open generic or closed).
    /// </summary>
    private static void ReportInvalidOverrideTargets(
        ImmutableArray<OpenGenericHandlerModel> openHandlers,
        ImmutableArray<HandlerModel> closedHandlers,
        ImmutableArray<Diagnostic>.Builder diagnostics
    )
    {
        var knownOpenKeys = new HashSet<string>(
            openHandlers.Select(static h => h.HandlerDefinitionKey),
            System.StringComparer.Ordinal
        );
        var knownClosedKeys = new HashSet<string>(
            closedHandlers.Select(static h => h.HandlerTypeName),
            System.StringComparer.Ordinal
        );

        foreach (var handler in openHandlers)
        {
            for (var i = 0; i < handler.OverrideTargetKeys.Length; i++)
            {
                var key = handler.OverrideTargetKeys[i];
                if (knownOpenKeys.Contains(key) || knownClosedKeys.Contains(key))
                {
                    continue;
                }

                diagnostics.Add(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.HandlerOverrideInvalidTarget,
                        handler.Location,
                        handler.HandlerDisplayName,
                        handler.OverrideTargetDisplayNames[i]
                    )
                );
            }
        }
    }

    /// <summary>Whole-word-replaces every key in <paramref name="substitutions"/> found in <paramref name="template"/> with its bound value, in one pass.</summary>
    private static string Substitute(string template, ImmutableDictionary<string, string> substitutions)
    {
        if (substitutions.IsEmpty)
        {
            return template;
        }

        var pattern =
            "\\b(" + string.Join("|", substitutions.Keys.OrderByDescending(static k => k.Length).Select(Regex.Escape)) + ")\\b";
        return Regex.Replace(template, pattern, match => substitutions[match.Value]);
    }

    /// <summary>One open generic handler successfully unified against one closed usage, with its per-type-parameter substitutions.</summary>
    private sealed record MatchedHandler(OpenGenericHandlerModel Handler, ImmutableDictionary<string, string> Substitutions);

    /// <summary>The outcome of <see cref="Resolve"/>: every successfully closed handler, plus any diagnostics.</summary>
    /// <param name="ClosedHandlers">One <see cref="HandlerModel"/> per unambiguously resolved open-handler/closed-usage match.</param>
    /// <param name="Diagnostics">Ambiguity and invalid-override-target diagnostics, if any.</param>
    /// <param name="AttemptedUsageDisplayNames">
    /// Every usage's <see cref="ClosedGenericRequestUsageModel.ClosedRequestDisplayName"/> at least one local
    /// open generic handler's request definition key matched, whether or not resolution actually succeeded -
    /// see <see cref="Resolve"/>'s remarks on why <see cref="HandlerDiagnosticAnalyzer.ReportUnresolvedGenericRequestUsages"/>
    /// needs this to avoid double-reporting an already-ambiguous usage.
    /// </param>
    public sealed record Result(
        ImmutableArray<HandlerModel> ClosedHandlers,
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<string> AttemptedUsageDisplayNames
    );
}
