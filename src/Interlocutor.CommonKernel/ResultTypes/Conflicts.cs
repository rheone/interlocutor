namespace Interlocutor.CommonKernel.ResultTypes;

using Interlocutor.CommonKernel.ResultTypes.Internal;

/// <summary>
/// Represents an immutable collection of one or more conflict outcomes.
/// </summary>
/// <remarks>
/// <para>
/// Similar to <see cref="Failures"/>, this type aggregates multiple <see cref="Conflict"/> instances
/// without duplication. Use this when an operation encounters multiple conflicting states that
/// all prevent progress and should all be reported together.
/// </para>
/// <para>
/// Duplicate conflicts are automatically removed, preserving order of first occurrence.
/// </para>
/// </remarks>
public readonly record struct Conflicts
{
    private readonly DistinctItemSet<Conflict> _conflicts;

    /// <summary>
    /// Initializes a new instance of the <see cref="Conflicts"/> struct from the specified conflict values.
    /// </summary>
    /// <param name="conflicts">
    /// The conflict values that should be collected. Duplicate conflicts are removed, preserving
    /// the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="conflicts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="conflicts"/> contains no elements.</exception>
    public Conflicts(IEnumerable<Conflict> conflicts)
    {
        this._conflicts = new DistinctItemSet<Conflict>(conflicts, "At least one conflict is required.", nameof(conflicts));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Conflicts"/> struct from a single conflict.
    /// </summary>
    /// <param name="conflict">The conflict value.</param>
    public Conflicts(Conflict conflict)
        : this([conflict]) { }

    /// <summary>
    /// Gets the number of distinct conflicts.
    /// </summary>
    /// <value>The count of distinct conflicts, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="Conflicts"/> (zero-initialized) is inert and reports a count of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._conflicts.Count;

    /// <summary>
    /// Gets the distinct set of conflicts, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="Conflict"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="Conflicts"/> (zero-initialized) is inert and returns an empty collection,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<Conflict> Items => this._conflicts.Items;

    /// <summary>
    /// Gets the distinct set of conflict messages, in the order they were first encountered.
    /// Semantic projection of <see cref="Items"/>.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct messages found in <see cref="Items"/>, or an empty collection
    /// if this instance is the default value.
    /// </value>
    public IReadOnlyList<string> Messages => this.Items.Count == 0 ? [] : [.. this.Items.Select(c => c.Message).Distinct()];
}
