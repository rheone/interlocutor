namespace Mediance.CommonKernel;

using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a result union representing an operation's outcome.
/// </summary>
public interface IResult;

/// <summary>
/// Represents the outcome of a void operation as one of two possible cases: success with no payload, or failure.
/// </summary>
/// <remarks>
/// <para>This is a union declaration with two case types: <see cref="Success"/> — the operation completed
/// successfully, or <see cref="Failure"/> — the operation failed.</para>
/// <para>Values of either case type convert implicitly to <see cref="Result"/>, and switch expressions over it
/// are exhaustive without a discard arm.</para>
/// </remarks>
public readonly union Result(Success, Failure) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <see cref="Success"/>; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is Success;
}

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
public readonly union Result<TSuccessResult, TFailureResult>(TSuccessResult, TFailureResult) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparam name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is TSuccessResult;
}

/// <summary>
/// Represents the outcome of an operation as one of three possible cases: a successful result or one of two result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="T2">The type of the first alternative result case.</typeparam>
/// <typeparam name="T3">The type of the second alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with three case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="T2"/> — the operation
/// failed with the first result case.</description></item> <item><description><typeparamref name="T3"/> — the operation
/// failed with the second result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="Result{TSuccessResult, T2, T3}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union Result<TSuccessResult, T2, T3>(TSuccessResult, T2, T3) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparam name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> on failure.</returns>
    public object? GetValue() => this switch
    {
        TSuccessResult success => success,
        T2 t2 => t2,
        T3 t3 => t3,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of four possible cases: a successful result or one of three result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="T2">The type of the first alternative result case.</typeparam>
/// <typeparam name="T3">The type of the second alternative result case.</typeparam>
/// <typeparam name="T4">The type of the third alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with four case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="T2"/> — the operation
/// failed with the first result case.</description></item> <item><description><typeparamref name="T3"/> — the operation
/// failed with the second result case.</description></item> <item><description><typeparamref name="T4"/> — the operation
/// failed with the third result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="Result{TSuccessResult, T2, T3, T4}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union Result<TSuccessResult, T2, T3, T4>(TSuccessResult, T2, T3, T4) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparam name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> on failure.</returns>
    public object? GetValue() => this switch
    {
        TSuccessResult success => success,
        T2 t2 => t2,
        T3 t3 => t3,
        T4 t4 => t4,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of five possible cases: a successful result or one of four result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="T2">The type of the first alternative result case.</typeparam>
/// <typeparam name="T3">The type of the second alternative result case.</typeparam>
/// <typeparam name="T4">The type of the third alternative result case.</typeparam>
/// <typeparam name="T5">The type of the fourth alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with five case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="T2"/> — the operation
/// failed with the first result case.</description></item> <item><description><typeparamref name="T3"/> — the operation
/// failed with the second result case.</description></item> <item><description><typeparamref name="T4"/> — the operation
/// failed with the third result case.</description></item> <item><description><typeparamref name="T5"/> — the operation
/// failed with the fourth result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="Result{TSuccessResult, T2, T3, T4, T5}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union Result<TSuccessResult, T2, T3, T4, T5>(TSuccessResult, T2, T3, T4, T5) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparam name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> on failure.</returns>
    public object? GetValue() => this switch
    {
        TSuccessResult success => success,
        T2 t2 => t2,
        T3 t3 => t3,
        T4 t4 => t4,
        T5 t5 => t5,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of five possible cases: a successful result or one of four failure
/// types.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="T2">The type of the first alternative result case.</typeparam>
/// <typeparam name="T3">The type of the second alternative result case.</typeparam>
/// <typeparam name="T4">The type of the third alternative result case.</typeparam>
/// <typeparam name="T5">The type of the fourth alternative result case.</typeparam>
/// <typeparam name="T6">The type of the fourth alternative result case.</typeparam>
/// <remarks>
///
/// <para>This is a union declaration with six case types: </para>
///
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="T2"/> — the operation failed with the
/// first result case.</description></item> <item><description><typeparamref name="T3"/> — the operation failed with
/// the second result case.</description></item> <item><description><typeparamref name="T4"/> — the operation failed
/// with the third result case.</description></item> <item><description><typeparamref name="T5"/> — the operation
/// failed with the fourth result case.</description></item> </list>
///
/// <para>Values of any case type convert implicitly to <see cref="Result{TSuccessResult, T2, T3, T4, T5, T6}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union Result<TSuccessResult, T2, T3, T4, T5, T6>(TSuccessResult, T2, T3, T4, T5, T6) : IResult
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparam name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> on failure.</returns>
    public object? GetValue() => this switch
    {
        TSuccessResult success => success,
        T2 t2 => t2,
        T3 t3 => t3,
        T4 t4 => t4,
        T5 t5 => t5,
        T6 t6 => t6,
        _ => null
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
