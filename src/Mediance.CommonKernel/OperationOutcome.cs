namespace Mediance.CommonKernel;

using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Represents the outcome of a void operation as one of two possible cases: success with no payload, or failure.
/// </summary>
/// <remarks>
/// <para>This is a union declaration with two case types: <see cref="Success"/> — the operation completed
/// successfully, or <see cref="Failure"/> — the operation failed.</para>
/// <para>Values of either case type convert implicitly to <see cref="OperationOutcome"/>, and switch expressions
/// over it are exhaustive without a discard arm.</para>
/// </remarks>
public readonly union OperationOutcome(Success, Failure) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <see cref="Success"/>; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is Success;
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
/// <para>Values of either case type convert implicitly to <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/>,
/// and switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union OperationOutcome<TSuccessResult, TFailureResult>(TSuccessResult, TFailureResult) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparamref name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is TSuccessResult;
}

/// <summary>
/// Represents the outcome of an operation as one of three possible cases: a successful result or one of two result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="TCase2">The type of the first alternative result case.</typeparam>
/// <typeparam name="TCase3">The type of the second alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with three case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="TCase2"/> — the operation
/// resolved to the first alternative result case.</description></item> <item><description><typeparamref name="TCase3"/> — the operation
/// resolved to the second alternative result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union OperationOutcome<TSuccessResult, TCase2, TCase3>(TSuccessResult, TCase2, TCase3) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparamref name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> if the case is not <typeparamref name="TSuccessResult"/>.</returns>
    public object? GetValue() => this.Value switch
    {
        TSuccessResult success => success,
        TCase2 case2 => case2,
        TCase3 case3 => case3,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of four possible cases: a successful result or one of three result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="TCase2">The type of the first alternative result case.</typeparam>
/// <typeparam name="TCase3">The type of the second alternative result case.</typeparam>
/// <typeparam name="TCase4">The type of the third alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with four case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="TCase2"/> — the operation
/// resolved to the first alternative result case.</description></item> <item><description><typeparamref name="TCase3"/> — the operation
/// resolved to the second alternative result case.</description></item> <item><description><typeparamref name="TCase4"/> — the operation
/// resolved to the third alternative result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4>(TSuccessResult, TCase2, TCase3, TCase4) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparamref name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> if the case is not <typeparamref name="TSuccessResult"/>.</returns>
    public object? GetValue() => this.Value switch
    {
        TSuccessResult success => success,
        TCase2 case2 => case2,
        TCase3 case3 => case3,
        TCase4 case4 => case4,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of five possible cases: a successful result or one of four result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="TCase2">The type of the first alternative result case.</typeparam>
/// <typeparam name="TCase3">The type of the second alternative result case.</typeparam>
/// <typeparam name="TCase4">The type of the third alternative result case.</typeparam>
/// <typeparam name="TCase5">The type of the fourth alternative result case.</typeparam>
/// <remarks>
/// <para>This is a union declaration with five case types: </para>
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="TCase2"/> — the operation
/// resolved to the first alternative result case.</description></item> <item><description><typeparamref name="TCase3"/> — the operation
/// resolved to the second alternative result case.</description></item> <item><description><typeparamref name="TCase4"/> — the operation
/// resolved to the third alternative result case.</description></item> <item><description><typeparamref name="TCase5"/> — the operation
/// resolved to the fourth alternative result case.</description></item> </list>
/// <para>Values of any case type convert implicitly to <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>(TSuccessResult, TCase2, TCase3, TCase4, TCase5) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparamref name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> if the case is not <typeparamref name="TSuccessResult"/>.</returns>
    public object? GetValue() => this.Value switch
    {
        TSuccessResult success => success,
        TCase2 case2 => case2,
        TCase3 case3 => case3,
        TCase4 case4 => case4,
        TCase5 case5 => case5,
        _ => null
    };
}

/// <summary>
/// Represents the outcome of an operation as one of six possible cases: a successful result or one of five alternative
/// result cases.
/// </summary>
/// <typeparam name="TSuccessResult">The type produced when the operation succeeds.</typeparam>
/// <typeparam name="TCase2">The type of the first alternative result case.</typeparam>
/// <typeparam name="TCase3">The type of the second alternative result case.</typeparam>
/// <typeparam name="TCase4">The type of the third alternative result case.</typeparam>
/// <typeparam name="TCase5">The type of the fourth alternative result case.</typeparam>
/// <typeparam name="TCase6">The type of the fifth alternative result case.</typeparam>
/// <remarks>
///
/// <para>This is a union declaration with six case types: </para>
///
/// <list type="bullet"><item><description><typeparamref name="TSuccessResult"/> — the operation completed
/// successfully.</description></item> <item><description><typeparamref name="TCase2"/> — the operation resolved to the
/// first alternative result case.</description></item> <item><description><typeparamref name="TCase3"/> — the operation resolved to
/// the second alternative result case.</description></item> <item><description><typeparamref name="TCase4"/> — the operation resolved
/// to the third alternative result case.</description></item> <item><description><typeparamref name="TCase5"/> — the operation
/// resolved to the fourth alternative result case.</description></item> <item><description><typeparamref name="TCase6"/> — the operation
/// resolved to the fifth alternative result case.</description></item> </list>
///
/// <para>Values of any case type convert implicitly to <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6}"/>, and
/// switch expressions over it are exhaustive without a discard arm. </para>
/// </remarks>
public readonly union OperationOutcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6>(TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6) : IOperationOutcome
{
    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this result contains <typeparamref name="TSuccessResult"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    public bool IsSuccess => this.Value is TSuccessResult;

    /// <summary>
    /// Extracts the underlying value from any case of this result, or <see langword="null"/> if extraction is not possible.
    /// </summary>
    /// <returns>The value held by this result, regardless of which case it is, or <see langword="null"/> if the case is not <typeparamref name="TSuccessResult"/>.</returns>
    public object? GetValue() => this.Value switch
    {
        TSuccessResult success => success,
        TCase2 case2 => case2,
        TCase3 case3 => case3,
        TCase4 case4 => case4,
        TCase5 case5 => case5,
        TCase6 case6 => case6,
        _ => null
    };
}
