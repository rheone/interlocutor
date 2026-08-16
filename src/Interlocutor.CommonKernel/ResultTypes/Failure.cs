namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Represents a generic failure outcome, optionally carrying a reason, an error code, or both.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Failure"/> when you need a generic, untyped failure result. It can carry either a human-readable
/// reason, a machine-readable error code, or both. This is the most flexible failure type - when you don't want to
/// construct a specific failure case like <see cref="NotFound"/> or <see cref="Forbidden"/>.
/// </para>
/// <para>
/// For operation outcome unions, <see cref="Failure"/> is a common fallback case when operation-specific failures
/// (like <see cref="ValidationErrors"/>) don't apply. It leaves room for unexpected errors without making callers
/// handle a dozen specific failure types.
/// </para>
/// </remarks>
public readonly record struct Failure
{
    /// <summary>
    /// Gets the human-readable reason for the failure, or <see langword="null"/> if none was provided.
    /// </summary>
    /// <value>The failure reason, or <see langword="null"/> if none was set.</value>
    public string? Reason { get; }

    /// <summary>
    /// Gets the error code associated with the failure, or <see langword="null"/> if none was provided.
    /// </summary>
    /// <value>The failure error code, or <see langword="null"/> if none was set.</value>
    public long? ErrorCode { get; }

    /// <summary>
    /// Gets a value indicating whether this failure has a <see cref="Reason"/>.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Reason"/> is not <see langword="null"/>; otherwise, <see langword="false"/>.</value>
    public bool HasReason => this.Reason is not null;

    /// <summary>
    /// Gets a value indicating whether this failure has an <see cref="ErrorCode"/>.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="ErrorCode"/> is not <see langword="null"/>; otherwise, <see langword="false"/>.</value>
    public bool HasErrorCode => this.ErrorCode is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failure"/> struct.
    /// Initializes a new instance of the <see cref="Failure"/> class with the specified reason.
    /// </summary>
    /// <param name="reason">The human-readable reason for the failure.</param>
    public Failure(string reason) => this.Reason = reason;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failure"/> struct.
    /// Initializes a new instance of the <see cref="Failure"/> class with the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code associated with the failure.</param>
    public Failure(long errorCode) => this.ErrorCode = errorCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="Failure"/> struct.
    /// Initializes a new instance of the <see cref="Failure"/> class with the specified reason and error code.
    /// </summary>
    /// <param name="reason">The human-readable reason for the failure.</param>
    /// <param name="errorCode">The error code associated with the failure.</param>
    public Failure(string reason, long errorCode) => (this.Reason, this.ErrorCode) = (reason, errorCode);

    ///// <summary>
    ///// Returns a string representation of the failure, including the reason and/or error code when present.
    ///// </summary>
    ///// <returns>
    ///// <c>"Failure(ErrorCode): Reason"</c> if both are set; <c>"Failure: Reason"</c> if only <see cref="Reason"/>
    ///// is set; <c>"Failure(ErrorCode)"</c> if only <see cref="ErrorCode"/> is set; otherwise <c>"Failure"</c>.
    ///// </returns>
    //public override string ToString() =>
    //    this switch
    //    {
    //        { Reason: not null, ErrorCode: not null } => $"Failure({ErrorCode}): {Reason}",
    //        { Reason: not null } => $"Failure: {Reason}",
    //        { ErrorCode: not null } => $"Failure({ErrorCode})",
    //        _ => "Failure",
    //    };
}
