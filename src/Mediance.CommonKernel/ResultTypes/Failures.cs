namespace Mediance.CommonKernel.ResultTypes;

using System.Collections.Immutable;

/// <summary>
/// Represents an immutable collection of one or more generic failure reasons.
/// </summary>
/// <remarks>
/// <para>
/// Similar to <see cref="ValidationErrors"/>, this type aggregates multiple <see cref="Failure"/> instances
/// without relying on property names or error codes — purely reason messages. Use this when you need to
/// return multiple independent failure reasons from a handler.
/// </para>
/// <para>
/// Duplicate failures are automatically removed, preserving order of first occurrence.
/// </para>
/// </remarks>
public readonly record struct Failures
{
    private readonly ImmutableArray<Failure> _failures;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures"/> class.
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
        ArgumentNullException.ThrowIfNull(failures);

        this._failures = [.. failures.Distinct()];

        if (this._failures.IsEmpty)
        {
            throw new ArgumentException("At least one failure is required.", nameof(failures));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Failures"/> class.
    /// Initializes a new instance of the <see cref="Failures"/> struct from a single failure.
    /// </summary>
    /// <param name="failure">The failure value.</param>
    public Failures(Failure failure)
        : this([failure]) { }

    /// <summary>
    /// Gets the number of distinct failures.
    /// </summary>
    /// <value>The count of distinct failures.</value>
    public int Count => this._failures.Length;

    /// <summary>
    /// Gets the distinct set of failures, in the order they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct <see cref="Failure"/> values provided at creation.</value>
    public IReadOnlyList<Failure> Values => this._failures;
}
