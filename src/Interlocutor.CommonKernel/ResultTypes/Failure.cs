namespace Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402 // Allowing multiple types of same name with differet arity

/// <summary>
/// Represents a generic failure outcome consisting of a reason only, with no associated error code.
/// </summary>
/// <param name="Reason">The human-readable reason for the failure, or <see langword="null"/> if none is provided.</param>
/// <remarks>
/// <para>
/// Use <see cref="Failure"/> when you need a generic, untyped failure result. This is the most flexible failure
/// type - when you don't want to construct a specific failure case like <see cref="NotFound"/> or
/// <see cref="Forbidden"/>.
/// </para>
/// <para>
/// For operation outcome unions, <see cref="Failure"/> is a common fallback case when operation-specific failures
/// (like <see cref="ValidationErrors"/>) don't apply. It leaves room for unexpected errors without making callers
/// handle a dozen specific failure types.
/// </para>
/// <para>
/// For error codes, use <see cref="Failure{TErrorCode}"/> instead. Aggregate multiple <see cref="Failure"/>
/// instances with <see cref="Failures"/>.
/// </para>
/// </remarks>
public readonly record struct Failure(string? Reason = null)
{
    /// <summary>
    /// Returns a human-readable representation of this failure.
    /// </summary>
    /// <returns><c>"Failure"</c> when <see cref="Reason"/> is <see langword="null"/>; otherwise <c>"Failure: {Reason}"</c>.</returns>
    public override string ToString() => this.Reason is null ? "Failure" : $"Failure: {this.Reason}";
}

/// <summary>
/// Represents a generic failure outcome consisting of an error code and, optionally, a reason.
/// </summary>
/// <typeparam name="TErrorCode">
/// The type used to represent the error code. Must support equality comparison so that error codes
/// can be reliably distinguished as unique.
/// </typeparam>
/// <param name="ErrorCode">The error code associated with the failure.</param>
/// <param name="Reason">The human-readable reason for the failure, or <see langword="null"/> if none is provided.</param>
/// <remarks>
/// <para>
/// Use <see cref="Failure{TErrorCode}"/> when you need both a machine-readable error code and, optionally, a
/// human-readable reason (e.g., for localization, retry logic, or programmatic error handling). The error code
/// type is generic, so you can use enums, strings, or composite types.
/// </para>
/// <para>
/// Example: <c>Failure&lt;string&gt;("payment_declined", "The card was declined")</c>.
/// </para>
/// </remarks>
public readonly record struct Failure<TErrorCode>(TErrorCode ErrorCode, string? Reason = null)
    where TErrorCode : notnull, IEquatable<TErrorCode>
{
    /// <summary>
    /// Returns a human-readable representation of this failure.
    /// </summary>
    /// <returns>
    /// <c>"Failure: {ErrorCode}"</c> when <see cref="Reason"/> is <see langword="null"/>; otherwise
    /// <c>"Failure: {ErrorCode} - {Reason}"</c>.
    /// </returns>
    public override string ToString() =>
        this.Reason is null ? $"Failure: {this.ErrorCode}" : $"Failure: {this.ErrorCode} - {this.Reason}";
}
