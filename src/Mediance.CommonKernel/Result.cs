namespace Mediance.CommonKernel;

/// <summary>
/// Represents the result of an operation that produces a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly union Result<T>(T, Failure);

/// <summary>
/// Represents the result of a void operation that produces no return value.
/// </summary>
public readonly union Result(Success, Failure);

/// <summary>
/// Represents a paginated subset of results from a query.
/// </summary>
/// <typeparam name="T">The type of items on the page.</typeparam>
/// <param name="items">The items on the current page.</param>
/// <param name="page">The one-based page index.</param>
/// <param name="pageSize">The maximum number of items per page.</param>
/// <param name="totalCount">The total number of items matching the query across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    /// <value>
    /// The total number of pages.
    /// </value>
    public int TotalPages => this.pageSize <= 0 ? 0 : (int)Math.Ceiling(this.totalCount / (double)this.pageSize);
    // Guards against DivideByZeroException when pageSize is 0 and treats invalid values as empty.

    /// <summary>
    /// Gets a value indicating whether a subsequent page of results exists.
    /// </summary>
    /// <value>
    /// A value indicating whether a subsequent page of results exists.
    /// </value>
    public bool HasNextPage => (long)this.page * this.pageSize < this.totalCount;
    // The explicit cast to long prevents overflow from the multiplication of two int values.
}
