namespace Mediance.CommonKernel.ResultTypes;

public readonly record struct Result<TResult>(TResult result);

/// <summary>
/// Represents the outcome of an operation as one of two possible cases: a successful result or a failure result.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="TFailureResult">The type produced when the operation fails.</typeparam>
/// <remarks>
/// <para>This is a union declaration with two case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="TFailureResult"/> — the operation
/// failed.</description></item> </list>
/// <para>Values of either case type convert implicitly to <see cref="Result{TSuccessResult, TFailureResult}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union Result<TSuccessResult, TFailureResult>(TSuccessResult, TFailureResult)
{
    /// <summary>
    /// Gets a value indicating whether this result holds a <typeparamref name="TSuccessResult"/>.
    /// </summary>
    public bool IsSuccess => this switch
    {
        TSuccessResult => true,
        _ => false,
    };
}

public readonly union Result(Success, Failure)
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    public bool IsSuccess => this switch
    {
        Success => true,
        _ => false,
    };
}

public readonly union Result<TSuccessResult, T2, T3>(TSuccessResult, T2, T3)
{
    public bool IsSuccess => this switch
    {
        TSuccessResult => true,
        _ => false
    };
}

public readonly union Result<TSuccessResult, T2, T3, T4>(TSuccessResult, T2, T3, T4)
{
    public bool IsSuccess => this switch
    {
        TSuccessResult => true,
        _ => false
    };
}

public readonly union Result<TSuccessResult, T2, T3, T4, T5>(TSuccessResult, T2, T3, T4, T5)
{
    public bool IsSuccess => this switch
    {
        TSuccessResult => true,
        _ => false
    };
}




///// <summary>
///// Represents the result of an operation that produces a value of type <typeparamref name="TResult"/>.
///// </summary>
///// <typeparam name="TResult">The type of the success value.</typeparam>
//public readonly union Result<TResult>(TResult, Failure);

///// <summary>
///// Represents the result of a void operation that produces no return value.
///// </summary>
//public readonly union Result(Success, Failure);

///// <summary>
///// Represents a paginated subset of results from a query.
///// </summary>
///// <typeparam name="T">The type of items on the page.</typeparam>
///// <param name="Items">The items on the current page.</param>
///// <param name="Page">The one-based page index.</param>
///// <param name="PageSize">The maximum number of items per page.</param>
///// <param name="TotalCount">The total number of items matching the query across all pages.</param>
//public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
//{
//    /// <summary>
//    /// Gets the total number of pages.
//    /// </summary>
//    /// <value>
//    /// The total number of pages.
//    /// </value>
//    public int TotalPages => this.PageSize <= 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)this.PageSize); // Guards against DivideByZeroException when pageSize is 0 and treats invalid values as empty.


//    /// <summary>
//    /// Gets a value indicating whether a subsequent page of results exists.
//    /// </summary>
//    /// <value>
//    /// A value indicating whether a subsequent page of results exists.
//    /// </value>
//    public bool HasNextPage => (long)this.Page * this.PageSize < this.TotalCount;     // The explicit cast to long prevents overflow from the multiplication of two int values.

// }
