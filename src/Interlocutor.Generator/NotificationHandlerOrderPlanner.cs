namespace Interlocutor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Computes the compile-time relative order across notification handlers declared via
/// <c>NotificationHandlerRunAfterAttribute</c>/<c>NotificationHandlerRunBeforeAttribute</c> (ADR-0007), by
/// topologically sorting the dependency graph those attributes describe.
/// </summary>
/// <remarks>
/// Only handler types that appear in at least one run-after/run-before relationship are assigned a rank -
/// every other handler is left out of <see cref="Result.Ranks"/> entirely, which
/// <see cref="NotificationHandlerOrderEmitter"/> and the runtime order table both treat as "unranked" (sorts
/// after every ranked handler, otherwise unspecified). A cycle is reported as
/// <see cref="InterlocutorDiagnostics.NotificationHandlerOrderingCycle"/> rather than producing a partial or
/// arbitrary order.
/// </remarks>
internal static class NotificationHandlerOrderPlanner
{
    /// <summary>Computes the topological order across every discovered notification handler's ordering attributes.</summary>
    /// <param name="notificationHandlers">Every <see cref="HandlerKind.Notification"/> handler discovered in the compilation.</param>
    /// <returns>The computed ranks, or a cycle diagnostic and no ranks if no topological order exists.</returns>
    public static Result Compute(ImmutableArray<HandlerModel> notificationHandlers)
    {
        var declaringLocation = new Dictionary<string, Location>(StringComparer.Ordinal);
        var edges = new HashSet<(string Before, string After)>();
        var nodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in notificationHandlers)
        {
            declaringLocation[handler.HandlerTypeName] = handler.Location;

            // A handler only becomes a node in the graph - and therefore only receives a rank - if it
            // actually participates in a run-after/run-before relationship, either as the declaring handler
            // or as the type another handler names. A handler with no ordering attribute at all, and named by
            // none, has no business appearing in the emitted order table (see Result's remarks).
            foreach (var predecessor in handler.RunAfterOrEmpty)
            {
                nodes.Add(predecessor);
                nodes.Add(handler.HandlerTypeName);
                edges.Add((predecessor, handler.HandlerTypeName));
            }

            foreach (var successor in handler.RunBeforeOrEmpty)
            {
                nodes.Add(successor);
                nodes.Add(handler.HandlerTypeName);
                edges.Add((handler.HandlerTypeName, successor));
            }
        }

        if (edges.Count == 0)
        {
            return new Result(ImmutableDictionary<string, int>.Empty, []);
        }

        var successors = edges
            .GroupBy(e => e.Before, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.After).ToImmutableArray(), StringComparer.Ordinal);
        var inDegree = nodes.ToDictionary(n => n, _ => 0, StringComparer.Ordinal);
        foreach (var (_, after) in edges)
        {
            inDegree[after]++;
        }

        // Kahn's algorithm, always picking the lexicographically smallest ready node so the resulting order -
        // and therefore the emitted source - is deterministic across generator runs, not merely "a valid
        // topological order" that happens to depend on hash-set iteration order.
        var order = ImmutableArray.CreateBuilder<string>();
        var remaining = new Dictionary<string, int>(inDegree, StringComparer.Ordinal);
        while (true)
        {
            var ready = remaining
                .Where(kv => kv.Value == 0)
                .Select(kv => kv.Key)
                .OrderBy(n => n, StringComparer.Ordinal)
                .FirstOrDefault();
            if (ready is null)
            {
                break;
            }

            order.Add(ready);
            remaining.Remove(ready);
            if (successors.TryGetValue(ready, out var next))
            {
                foreach (var successor in next)
                {
                    remaining[successor]--;
                }
            }
        }

        if (remaining.Count > 0)
        {
            var cycleMembers = remaining.Keys.OrderBy(n => n, StringComparer.Ordinal).ToImmutableArray();
            var anchor =
                cycleMembers
                    .Select(m => declaringLocation.TryGetValue(m, out var loc) ? loc : null)
                    .FirstOrDefault(l => l is not null)
                ?? Location.None;

            var diagnostic = Diagnostic.Create(
                InterlocutorDiagnostics.NotificationHandlerOrderingCycle,
                anchor,
                string.Join(" -> ", cycleMembers)
            );
            return new Result(ImmutableDictionary<string, int>.Empty, [diagnostic]);
        }

        var ranks = order
            .Select((handlerTypeName, index) => (handlerTypeName, index))
            .ToImmutableDictionary(t => t.handlerTypeName, t => t.index);
        return new Result(ranks, []);
    }

    /// <summary>The outcome of <see cref="Compute"/>: a rank per constrained handler type, plus any diagnostics.</summary>
    /// <param name="Ranks">Fully-qualified handler type name to its zero-based topological rank.</param>
    /// <param name="Diagnostics">Cycle diagnostics, if any ordering cycle was found.</param>
    public sealed record Result(ImmutableDictionary<string, int> Ranks, ImmutableArray<Diagnostic> Diagnostics);
}
