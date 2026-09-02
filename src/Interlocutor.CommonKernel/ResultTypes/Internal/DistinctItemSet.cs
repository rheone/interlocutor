namespace Interlocutor.CommonKernel.ResultTypes.Internal;

using System.Collections.Immutable;
using System.Diagnostics;

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
[DebuggerDisplay("Count = {Count}")]
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
        // paramName is a genuine, deliberate override point - each composing type (Conflicts, Failures, ...)
        // passes its own field name so a thrown exception reports that name instead of this constructor's
        // own "items" parameter. Passing it through explicitly means ThrowIfNull's own
        // [CallerArgumentExpression] auto-fill never kicks in (that only applies when the argument is
        // omitted at the call site), so the "unreachable"-message call sites that don't override it would
        // otherwise report a null ParamName instead of "items" - the ?? restores that default. S3236
        // flags any explicit argument here on principle, but omitting it would remove the override entirely.
#pragma warning disable S3236
        ArgumentNullException.ThrowIfNull(items, paramName ?? nameof(items));
#pragma warning restore S3236

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
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<TItem> Items => this._items.IsDefault ? [] : this._items;

    /// <inheritdoc/>
    /// <remarks>
    /// Overridden because the compiler-synthesized record equality this type would otherwise get compares
    /// <see cref="_items"/> - an <see cref="ImmutableArray{T}"/> - by reference to its backing array, not by
    /// content. Compared by content instead, and order-independently: two instances collected from the same
    /// distinct items are the same result even when concurrent/async work filled them in a different
    /// completion order. <see cref="Items"/> still preserves first-occurrence order for enumeration and
    /// display - a caller who needs a specific order for their own comparison can already impose one over
    /// <see cref="Items"/> themselves.
    /// </remarks>
    public bool Equals(DistinctItemSet<TItem> other) =>
        this.Count == other.Count && this.Items.ToHashSet().SetEquals(other.Items);

    /// <inheritdoc/>
    /// <remarks>
    /// Combined order-independently (XOR rather than a rolling multiply-and-add) to stay consistent with
    /// <see cref="Equals(DistinctItemSet{TItem})"/> - two instances holding the same distinct items in a
    /// different order must still produce the same hash code.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var item in this.Items)
        {
            hash ^= item?.GetHashCode() ?? 0;
        }

        return hash;
    }
}
