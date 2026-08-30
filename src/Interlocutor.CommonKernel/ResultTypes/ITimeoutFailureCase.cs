namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a timeout-shaped failure case usable in an <see cref="OperationOutcome"/> union.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IValidationFailureCase"/>'s role for validation: a minimal, zero-dependency contract
/// naming the shape any timeout rejection shares, ahead of a second implementation actually needing it.
/// <see cref="Timeout"/> implements it directly, and <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c>
/// recognizes this interface generically (ordered after its exact match on <see cref="Timeout"/>) - so a
/// future second implementation, first-party or consumer-authored, needs only its own case type and behavior;
/// no further retrofit is needed here or in the mapper.
/// </remarks>
public interface ITimeoutFailureCase
{
    /// <summary>
    /// Gets the human-readable description of the timeout.
    /// </summary>
    /// <value>The description, or <see langword="null"/> if none was supplied.</value>
    string? Message { get; }
}
