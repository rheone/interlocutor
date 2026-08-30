namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a timeout-shaped failure case usable in an <see cref="OperationOutcome"/> union.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IValidationFailureCase"/>'s role for validation: a minimal, zero-dependency contract
/// naming the shape any timeout rejection shares, ahead of a second implementation actually needing it.
/// <see cref="Timeout"/> implements it directly. No consumer pattern-matches on it yet -
/// <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c> still matches <see cref="Timeout"/> directly -
/// so a future second implementation needs only its own case type, behavior, and mapper recognition, not a
/// retrofit of this interface.
/// </remarks>
public interface ITimeoutFailureCase
{
    /// <summary>
    /// Gets the human-readable description of the timeout.
    /// </summary>
    /// <value>The description, or <see langword="null"/> if none was supplied.</value>
    string? Message { get; }
}
