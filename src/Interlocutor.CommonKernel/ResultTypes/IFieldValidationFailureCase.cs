namespace Interlocutor.CommonKernel.ResultTypes;

using System.Collections.Generic;

/// <summary>
/// Marks a validation-shaped failure case that can attribute each message to the property/field that failed,
/// beyond the plain message list <see cref="IValidationFailureCase"/> already guarantees.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <see cref="IValidationFailureCase"/>'s own role: a minimal, zero-dependency contract naming an
/// extra shape a validation failure case can carry, so a consumer that wants field-keyed output (an HTTP
/// <c>ValidationProblemDetails</c> body, for example) can recognize it generically without depending on the
/// validation technology that produced it. <see cref="ValidationErrors"/> does not implement this interface -
/// its <see cref="ValidationError"/> entries carry no property name - while
/// <c>Interlocutor.FluentValidation.FluentValidationErrors</c> does, since
/// <c>FluentValidation.Results.ValidationFailure.PropertyName</c> survives the pipeline unmodified.
/// </para>
/// <para>
/// A consumer that recognizes this interface - such as
/// <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c> - can build a field-keyed response for any
/// implementer without knowing about every concrete validation failure type that exists or might exist in the
/// future, falling back to the flat <see cref="IValidationFailureCase.Messages"/> shape for anything that
/// doesn't implement it.
/// </para>
/// </remarks>
public interface IFieldValidationFailureCase : IValidationFailureCase
{
    /// <summary>
    /// Gets each validation failure paired with the property/field it applies to, if any.
    /// </summary>
    /// <value>
    /// One entry per failure, in the same order as <see cref="IValidationFailureCase.Messages"/> - a failure
    /// not tied to a specific property carries a <see langword="null"/>
    /// <see cref="FieldValidationMessage.PropertyName"/>. Empty if this instance carries none.
    /// </value>
    IReadOnlyList<FieldValidationMessage> FieldMessages { get; }
}
