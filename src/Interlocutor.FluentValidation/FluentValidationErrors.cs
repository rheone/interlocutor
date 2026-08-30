namespace Interlocutor.FluentValidation;

using System.Collections.Generic;
using global::FluentValidation.Results;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Represents an immutable failure carrying one or more <see cref="ValidationFailure"/> instances verbatim,
/// with none of their fields discarded.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="FluentValidationErrors"/> as a failure case in operation outcome unions when a consumer
/// needs full fidelity from FluentValidation - <see cref="ValidationFailure.PropertyName"/>,
/// <see cref="ValidationFailure.Severity"/>, <see cref="ValidationFailure.ErrorCode"/>,
/// <see cref="ValidationFailure.CustomState"/>, and <see cref="ValidationFailure.AttemptedValue"/> all reach
/// the caller, rather than being collapsed to just <see cref="ValidationFailure.ErrorMessage"/> the way
/// <see cref="CommonKernel.ResultTypes.ValidationErrors"/> does.
/// </para>
/// <para>
/// Produced by <see cref="FluentValidationErrorsBehavior{TRequest, TResponse}"/> when one or more registered
/// validators report failures. Example outcome type:
/// <c>OperationOutcome&lt;UserDto, FluentValidationErrors&gt;</c>. Unlike
/// <see cref="CommonKernel.ResultTypes.ValidationErrors"/>, <see cref="Failures"/> is not de-duplicated -
/// every <see cref="ValidationFailure"/> reported by every validator is preserved verbatim, in the order
/// each validator produced it.
/// </para>
/// </remarks>
public readonly record struct FluentValidationErrors : IFieldValidationFailureCase
{
    private readonly IReadOnlyList<ValidationFailure>? _failures;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationErrors"/> struct from the specified
    /// validation failures.
    /// </summary>
    /// <param name="failures">
    /// The validation failures reported by one or more FluentValidation validators, preserved verbatim in
    /// the order supplied.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="failures"/> contains no elements.</exception>
    public FluentValidationErrors(IReadOnlyList<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count == 0)
        {
            throw new ArgumentException("At least one validation failure is required.", nameof(failures));
        }

        this._failures = failures;
    }

    /// <summary>
    /// Gets the validation failures reported by every validator, in the order each validator produced them.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the <see cref="ValidationFailure"/> instances provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="FluentValidationErrors"/> (zero-initialized) is inert and returns an
    /// empty collection, rather than throwing. This permits the pipeline to pass a default response value
    /// when a handler has not yet produced one. Explicitly constructing this type with an empty sequence
    /// still throws.
    /// </remarks>
    public IReadOnlyList<ValidationFailure> Failures => this._failures ?? [];

    /// <summary>
    /// Gets each failure's <see cref="ValidationFailure.ErrorMessage"/>, in the order <see cref="Failures"/>
    /// reports them.
    /// </summary>
    /// <value>
    /// The error messages carried by <see cref="Failures"/>, or an empty collection if this instance is the
    /// default value.
    /// </value>
    public IReadOnlyList<string> Messages => this.Failures.Count == 0 ? [] : [.. this.Failures.Select(f => f.ErrorMessage)];

    /// <summary>
    /// Gets each failure's <see cref="ValidationFailure.PropertyName"/> paired with its
    /// <see cref="ValidationFailure.ErrorMessage"/>, in the order <see cref="Failures"/> reports them.
    /// </summary>
    /// <value>
    /// The property/message pairs carried by <see cref="Failures"/>, or an empty collection if this instance
    /// is the default value. FluentValidation always populates <see cref="ValidationFailure.PropertyName"/> as
    /// an empty string rather than <see langword="null"/> for a rule with no target property, so this
    /// normalizes that to <see langword="null"/>, matching <see cref="FieldValidationMessage.PropertyName"/>'s
    /// documented meaning.
    /// </value>
    public IReadOnlyList<FieldValidationMessage> FieldMessages =>
        this.Failures.Count == 0
            ? []
            :
            [
                .. this.Failures.Select(f => new FieldValidationMessage(
                    string.IsNullOrEmpty(f.PropertyName) ? null : f.PropertyName,
                    f.ErrorMessage
                )),
            ];

    /// <summary>
    /// Returns a human-readable representation of this validation failure set.
    /// </summary>
    /// <returns>
    /// <c>"FluentValidationErrors"</c> when this instance is the default value (empty); otherwise
    /// <c>"FluentValidationErrors: {messages joined by ", "}"</c>.
    /// </returns>
    public override string ToString() =>
        this.Messages.Count == 0 ? "FluentValidationErrors" : $"FluentValidationErrors: {string.Join(", ", this.Messages)}";
}
