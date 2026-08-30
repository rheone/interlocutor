namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a forbidden-shaped failure case usable in an <see cref="OperationOutcome"/> union.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IValidationFailureCase"/>'s role for validation: a minimal, zero-dependency contract
/// naming the shape any authorization rejection shares, ahead of a second implementation actually needing it.
/// <see cref="Forbidden"/> implements it directly, and <c>Interlocutor.AspNetCore.DefaultOutcomeStatusMapper</c>
/// recognizes this interface generically (ordered after its exact match on <see cref="Forbidden"/>) - so a
/// future second implementation, first-party or consumer-authored, needs only its own case type and behavior;
/// no further retrofit is needed here or in the mapper.
/// </remarks>
public interface IForbiddenFailureCase
{
    /// <summary>
    /// Gets the description of why access was denied.
    /// </summary>
    /// <value>The description, or <see langword="null"/> if none was supplied.</value>
    string? Reason { get; }
}
