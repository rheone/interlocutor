namespace Interlocutor.CommonKernel.ResultTypes;

using System.Diagnostics;
using Interlocutor.CommonKernel.ResultTypes.Internal;

#pragma warning disable SA1402 // Allowing multiple types of same name with differet arity

/// <summary>
/// Represents an immutable collection of one or more generic failure reasons, with no associated error codes.
/// </summary>
/// <remarks>
/// <para>
/// Similar to <see cref="ValidationErrors"/>, this type aggregates multiple <see cref="Failure"/> instances
/// without relying on error codes - purely reason messages. Use this when you need to return multiple
/// independent failure reasons from a handler.
/// </para>
/// <para>
/// Duplicate failures are automatically removed, preserving order of first occurrence.
/// </para>
/// <para>
/// For failures that carry error codes, use <see cref="Failures{TErrorCode}"/>.
/// </para>
/// </remarks>
public readonly record struct Failures
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DistinctItemSet<Failure> _failures;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures"/> struct from the specified failure values.
    /// </summary>
    /// <param name="failures">
    /// The failure values that should be collected. Duplicate failures are removed, preserving
    /// the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="failures"/> contains no elements.</exception>
    public Failures(IEnumerable<Failure> failures)
    {
        this._failures = new DistinctItemSet<Failure>(failures, "At least one failure is required.", nameof(failures));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures"/> struct from a single failure.
    /// </summary>
    /// <param name="failure">The failure value.</param>
    public Failures(Failure failure)
        : this([failure]) { }

    /// <summary>
    /// Gets the number of distinct failures.
    /// </summary>
    /// <value>The count of distinct failures, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="Failures"/> (zero-initialized) is inert and reports a count of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._failures.Count;

    /// <summary>
    /// Gets the distinct set of failures, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="Failure"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="Failures"/> (zero-initialized) is inert and returns an empty collection,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<Failure> Items => this._failures.Items;

    /// <summary>
    /// Gets the distinct set of failure reasons, in the order they were first encountered.
    /// Semantic projection of <see cref="Items"/>.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct, non-<see langword="null"/> reasons found in <see cref="Items"/>,
    /// or an empty collection if this instance is the default value.
    /// </value>
    public IReadOnlyList<string> Reasons =>
        this.Items.Count == 0 ? [] : [.. this.Items.Where(f => f.Reason is not null).Select(f => f.Reason!).Distinct()];

    /// <summary>
    /// Returns a human-readable representation of this failure set.
    /// </summary>
    /// <returns>
    /// <c>"Failures"</c> when <see cref="Reasons"/> is empty (either because this instance is the default value,
    /// or because none of the contained failures carry a reason); otherwise <c>"Failures: {reasons joined by ", "}"</c>.
    /// Joins <see cref="Reasons"/> rather than each item's own <see cref="Failure.ToString"/>, to avoid a doubled
    /// "Failures: Failure: ..." prefix.
    /// </returns>
    public override string ToString() => this.Reasons.Count == 0 ? "Failures" : $"Failures: {string.Join(", ", this.Reasons)}";
}

/// <summary>
/// Represents an immutable collection of one or more generic failures, each carrying an error code and,
/// optionally, a reason.
/// </summary>
/// <typeparam name="TErrorCode">
/// The type used to represent each failure's error code. Must support equality comparison so that
/// error codes can be reliably distinguished as unique. Every failure supplied to a single
/// <see cref="Failures{TErrorCode}"/> instance must use this same <typeparamref name="TErrorCode"/> type;
/// this is enforced by the compiler since the type parameter is fixed for the entire instance.
/// </typeparam>
public readonly record struct Failures<TErrorCode>
    where TErrorCode : notnull, IEquatable<TErrorCode>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DistinctItemSet<Failure<TErrorCode>> _failures;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DistinctItemSet<TErrorCode> _uniqueErrorCodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures{TErrorCode}"/> struct from the specified failure values.
    /// </summary>
    /// <param name="failures">
    /// The failure values that should be collected. Duplicate error-code-and-reason pairs are removed,
    /// preserving the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="failures"/> contains no elements.</exception>
    public Failures(IEnumerable<Failure<TErrorCode>> failures)
    {
        this._failures = new DistinctItemSet<Failure<TErrorCode>>(
            failures,
            "At least one failure is required.",
            nameof(failures)
        );

        // Computed once here, rather than on every property access, preserving the order in which
        // each error code was first encountered in _failures.
        this._uniqueErrorCodes = new DistinctItemSet<TErrorCode>(this._failures.Items.Select(f => f.ErrorCode), "unreachable");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures{TErrorCode}"/> struct from a single failure.
    /// </summary>
    /// <param name="failure">The failure value.</param>
    public Failures(Failure<TErrorCode> failure)
        : this([failure]) { }

    /// <summary>
    /// Gets the number of distinct failures.
    /// </summary>
    /// <value>The count of distinct <see cref="Failure{TErrorCode}"/> values, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="Failures{TErrorCode}"/> (zero-initialized) is inert and reports a count of
    /// zero, rather than throwing. This permits the pipeline to pass a default response value when a handler has
    /// not yet produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._failures.Count;

    /// <summary>
    /// Gets the distinct set of failures, each with its error code and reason, in the order they were
    /// first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="Failure{TErrorCode}"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="Failures{TErrorCode}"/> (zero-initialized) is inert and returns an empty
    /// collection, rather than throwing. This permits the pipeline to pass a default response value when a
    /// handler has not yet produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<Failure<TErrorCode>> Items => this._failures.Items;

    /// <summary>
    /// Gets the distinct set of failure reasons, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct, non-<see langword="null"/> reasons found in <see cref="Items"/>,
    /// or an empty collection if this instance is the default value.
    /// </value>
    public IReadOnlyList<string> Reasons =>
        this.Items.Count == 0 ? [] : [.. this.Items.Where(f => f.Reason is not null).Select(f => f.Reason!).Distinct()];

    /// <summary>
    /// Gets the distinct set of error codes present in the original failures, in the order they were
    /// first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct error codes found in <see cref="Items"/>, or an empty collection
    /// if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="Failures{TErrorCode}"/> (zero-initialized) is inert and returns an empty
    /// collection, rather than throwing. This permits the pipeline to pass a default response value when a
    /// handler has not yet produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<TErrorCode> ErrorCodes => this._uniqueErrorCodes.Items;

    /// <summary>
    /// Returns a human-readable representation of this failure set.
    /// </summary>
    /// <returns>
    /// <c>"Failures"</c> when <see cref="Reasons"/> is empty (either because this instance is the default value,
    /// or because none of the contained failures carry a reason); otherwise <c>"Failures: {reasons joined by ", "}"</c>.
    /// Joins <see cref="Reasons"/> rather than each item's own <see cref="Failure{TErrorCode}.ToString"/>, to avoid
    /// a doubled "Failures: Failure: ..." prefix.
    /// </returns>
    public override string ToString() => this.Reasons.Count == 0 ? "Failures" : $"Failures: {string.Join(", ", this.Reasons)}";
}

#pragma warning restore SA1402
