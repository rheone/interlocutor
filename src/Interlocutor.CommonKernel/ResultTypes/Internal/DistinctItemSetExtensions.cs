namespace Interlocutor.CommonKernel.ResultTypes.Internal;

/// <summary>
/// Shared accessor shape for the plural aggregate result types that compose a <see cref="DistinctItemSet{TItem}"/>
/// (<see cref="Conflicts"/>, <see cref="Failures"/>, <see cref="Failures{TErrorCode}"/>,
/// <see cref="ValidationErrors"/>, <see cref="ValidationErrors{TErrorCode}"/>).
/// </summary>
/// <remarks>
/// Each composing type projects a semantic view over its own <see cref="DistinctItemSet{TItem}"/> - reasons,
/// messages - and formats an identically-shaped <see cref="object.ToString"/> from that projection. Because
/// <see langword="readonly record struct"/> types cannot inherit from a shared base class, that shape is
/// centralized here as extension members instead of every composing type hand-rolling its own copy of the
/// same distinct-projection-then-join logic.
/// </remarks>
internal static class DistinctItemSetExtensions
{
    extension<TItem>(DistinctItemSet<TItem> set)
    {
        /// <summary>
        /// Projects <see cref="DistinctItemSet{TItem}.Items"/> through <paramref name="selector"/>, removing
        /// duplicate projected values while preserving the order of first occurrence.
        /// </summary>
        /// <typeparam name="TProjected">The projected value type. Must support equality comparison so that
        /// duplicates can be detected.</typeparam>
        /// <param name="selector">The projection applied to each item.</param>
        /// <returns>
        /// An unmodifiable collection of the distinct projected values, or an empty collection if
        /// <paramref name="set"/> is the default value or has no items.
        /// </returns>
        public IReadOnlyList<TProjected> DistinctProjection<TProjected>(Func<TItem, TProjected> selector) =>
            set.Items.Count == 0 ? [] : [.. set.Items.Select(selector).Distinct()];

        /// <summary>
        /// Projects <see cref="DistinctItemSet{TItem}.Items"/> through <paramref name="selector"/>, discarding
        /// <see langword="null"/> projections and removing duplicate remaining values while preserving the
        /// order of first occurrence.
        /// </summary>
        /// <param name="selector">
        /// The projection applied to each item. A <see langword="null"/> result is dropped rather than being
        /// treated as a distinguishable value.
        /// </param>
        /// <returns>
        /// An unmodifiable collection of the distinct, non-<see langword="null"/> projected values, or an
        /// empty collection if <paramref name="set"/> is the default value, has no items, or every projection
        /// is <see langword="null"/>.
        /// </returns>
        public IReadOnlyList<string> DistinctProjectionWhereNotNull(Func<TItem, string?> selector) =>
            set.Items.Count == 0
                ? []
                : [.. set.Items.Select(selector).Where(value => value is not null).Select(value => value!).Distinct()];
    }

    /// <summary>
    /// Formats the human-readable representation shared by every plural aggregate result type.
    /// </summary>
    /// <param name="typeName">The composing type's own name, e.g. <c>"Failures"</c>.</param>
    /// <param name="values">The already-projected display values (e.g. reasons or messages) to join.</param>
    /// <returns>
    /// <paramref name="typeName"/> when <paramref name="values"/> is empty (either because the composing type
    /// is its default value, or because none of its items produced a display value); otherwise
    /// <c>"{typeName}: {values joined by ", "}"</c>.
    /// </returns>
    public static string ToDisplayString(string typeName, IReadOnlyList<string> values) =>
        values.Count == 0 ? typeName : $"{typeName}: {string.Join(", ", values)}";
}
