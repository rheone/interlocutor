namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a rate-limit-shaped failure case usable in an <see cref="OperationOutcome"/> union.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IValidationFailureCase"/>'s role for validation: a minimal, zero-dependency contract
/// naming the shape any rate-limit rejection shares, ahead of a second implementation actually needing it.
/// <see cref="RateLimited"/> implements it directly, and <c>Interlocutor.AspNetCore.DefaultOutcomeStatusMapper</c>
/// recognizes this interface generically (ordered after its exact match on <see cref="RateLimited"/>) - so a
/// future second implementation, first-party or consumer-authored, needs only its own case type and behavior;
/// no further retrofit is needed here or in the mapper.
/// </remarks>
public interface IRateLimitedFailureCase
{
    /// <summary>
    /// Gets the human-readable description of the rate limit that was exceeded.
    /// </summary>
    /// <value>The description, or <see langword="null"/> if none was supplied.</value>
    string? Message { get; }
}
