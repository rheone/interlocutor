namespace Interlocutor.CommonKernel.ResultTypes.Internal;

using System.Collections.Immutable;

/// <summary>
/// Shared storage for the plural aggregate result types (<see cref="Conflicts"/>, <see cref="Failures"/>,
/// <see cref="ValidationErrors"/>, <see cref="ValidationErrors{TErrorCode}"/>), each of which composes an
/// instance of this type instead of hand-rolling its own dedup/count/items logic.
/// </summary>
/// <typeparam name="TItem">The item type being collected. Must support equality comparison so that
/// duplicates can be detected.</typeparam>
/// <remarks>
/// <para>
/// Because <see langword="readonly record struct"/> types cannot inherit from a shared base class, the
/// plural result types share this behavior through composition: each wraps a private
/// <see cref="DistinctItemSet{TItem}"/> field rather than duplicating the validating-constructor,
/// dedup-preserving-order, and default-value-is-inert logic four times over.
/// </para>
/// </remarks>
internal readonly record struct DistinctItemSet<TItem>
{
    private readonly ImmutableArray<TItem> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistinctItemSet{TItem}"/> struct from the specified items.
    /// </summary>
    /// <param name="items">
    /// The items that should be collected. Duplicate items are removed, preserving the order of first
    /// occurrence.
    /// </param>
    /// <param name="emptyMessage">
    /// The message used for the <see cref="ArgumentException"/> thrown when <paramref name="items"/> contains
    /// no elements, so each composing type can surface its own type-specific wording.
    /// </param>
    /// <param name="paramName">The parameter name reported by any thrown exception.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="items"/> contains no elements.</exception>
    public DistinctItemSet(IEnumerable<TItem> items, string emptyMessage, string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(items, paramName);

        this._items = [.. items.Distinct()];

        if (this._items.IsEmpty)
        {
            throw new ArgumentException(emptyMessage, paramName);
        }
    }

    /// <summary>
    /// Gets the number of distinct items.
    /// </summary>
    /// <value>The count of distinct items, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="DistinctItemSet{TItem}"/> (zero-initialized) is inert and reports a
    /// count of zero, rather than throwing. This permits the composing type's own default value to behave the
    /// same way. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._items.IsDefault ? 0 : this._items.Length;

    /// <summary>
    /// Gets the distinct set of items, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct items provided at creation, or an empty collection if this
    /// instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="DistinctItemSet{TItem}"/> (zero-initialized) is inert and returns an
    /// empty collection, rather than throwing. This permits the composing type's own default value to behave
    /// the same way. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<TItem> Items => this._items.IsDefault ? [] : this._items;
}
