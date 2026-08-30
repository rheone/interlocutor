namespace Interlocutor.CommonKernel.ResultTypes;

using System.Collections.Generic;

/// <summary>
/// Marks a type as a validation-shaped failure case usable in an <see cref="OperationOutcome"/> union.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <see cref="IOperationOutcome"/> as an extension point: a minimal, zero-dependency contract any
/// validation-shaped failure case can implement, rather than every technology-specific validation failure
/// being forced through one concrete type. <see cref="ValidationErrors"/> implements it directly;
/// <c>Interlocutor.FluentValidation.FluentValidationErrors</c> implements it too, preserving the full fidelity
/// of a <c>FluentValidation.Results.ValidationFailure</c> rather than collapsing it to just its message.
/// </para>
/// <para>
/// A consumer that recognizes this interface - such as
/// <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c> - can map any implementer to the same outcome
/// (a 400 Bad Request response, for example) without needing to know about every concrete validation failure
/// type that exists or might exist in the future.
/// </para>
/// </remarks>
public interface IValidationFailureCase
{
    /// <summary>
    /// Gets the validation failure messages carried by this case.
    /// </summary>
    /// <value>
    /// The validation error messages describing why validation failed, or an empty collection if this
    /// instance carries none. Whether messages are de-duplicated is left to the implementer -
    /// <see cref="ValidationErrors"/> de-duplicates, but an implementer that preserves full fidelity from an
    /// underlying validation library (one message per rule violation, even if two happen to read identically)
    /// is not required to.
    /// </value>
    IReadOnlyList<string> Messages { get; }
}
