namespace Interlocutor.FluentValidation;

using System.Collections.Generic;
using System.Diagnostics;
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
/// <c>Outcome&lt;UserDto, FluentValidationErrors&gt;</c>. Unlike
/// <see cref="CommonKernel.ResultTypes.ValidationErrors"/>, <see cref="Failures"/> is not de-duplicated -
/// every <see cref="ValidationFailure"/> reported by every validator is preserved verbatim, in the order
/// each validator produced it.
/// </para>
/// </remarks>
public readonly record struct FluentValidationErrors : IFieldValidationFailureCase
{
    // Not DistinctItemSet<TItem>-typed like the other five aggregate types' backing fields (this type
    // preserves every failure verbatim rather than deduplicating), but hidden the same way for the same
    // reason: expanding this type in the debugger should show only its public Failures/Messages/FieldMessages
    // surface, not the private backing field a second time.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

    /// <summary>
    /// Determines whether this instance and <paramref name="other"/> hold the same <see cref="ValidationFailure"/>
    /// content, order-independently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the other five aggregate result types, this type composes a bare <c>IReadOnlyList&lt;ValidationFailure&gt;?</c>
    /// rather than <c>DistinctItemSet&lt;TItem&gt;</c> - deliberately, since it preserves every failure verbatim
    /// instead of deduplicating (see this type's own remarks) - so it cannot inherit
    /// <c>DistinctItemSet{TItem}</c>'s order-independent equality "for free" the way the other five do. This
    /// override gives it the same order-independent guarantee directly instead, as a multiset (bag) comparison
    /// that still respects duplicate counts: <c>[A, A, B]</c> and <c>[A, B]</c> are unequal, but <c>[A, B]</c>
    /// and <c>[B, A]</c> are equal.
    /// </para>
    /// <para>
    /// <see cref="ValidationFailure"/> is a FluentValidation type with no value equality of its own (a plain
    /// mutable class, compared by reference by default) - even the exact same instance, held in two distinct
    /// backing lists, would otherwise compare unequal. Each failure is instead compared by the same fields this
    /// type's own remarks document as its full-fidelity surface: <see cref="ValidationFailure.PropertyName"/>,
    /// <see cref="ValidationFailure.ErrorMessage"/>, <see cref="ValidationFailure.Severity"/>,
    /// <see cref="ValidationFailure.ErrorCode"/>, <see cref="ValidationFailure.CustomState"/>, and
    /// <see cref="ValidationFailure.AttemptedValue"/> - so two failures built separately (e.g. by a retried
    /// validation pass) compare equal as long as those fields agree, not only when they're literally the same
    /// instance.
    /// </para>
    /// </remarks>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><see langword="true"/> if both instances hold the same failures, ignoring order; otherwise, <see langword="false"/>.</returns>
    public bool Equals(FluentValidationErrors other)
    {
        var mine = this.Failures;
        var theirs = other.Failures;

        if (mine.Count != theirs.Count)
        {
            return false;
        }

        // O(n^2) multiset match rather than hash-bucketing: ValidationFailure has no GetHashCode of its own
        // to bucket by, and validation-failure lists are small enough that this is not a real cost.
        var matched = new bool[theirs.Count];
        foreach (var failure in mine)
        {
            var foundMatch = false;
            for (var i = 0; i < theirs.Count; i++)
            {
                if (!matched[i] && FailureContentEquals(failure, theirs[i]))
                {
                    matched[i] = true;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a hash code derived from every failure's content, order-independently.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(FluentValidationErrors)"/>.</returns>
    /// <remarks>
    /// Combined order-independently (XOR rather than a rolling multiply-and-add), the same way
    /// <c>DistinctItemSet{TItem}.GetHashCode</c> does, so two instances holding the same failures in a
    /// different order still produce the same hash code. <c>Equals(object?)</c>/<c>==</c>/<c>!=</c> are left
    /// to the record struct's own synthesis, which forwards to <see cref="Equals(FluentValidationErrors)"/>
    /// above the same way it already does for every other record struct in this library - only the two
    /// members that need custom logic are overridden here.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var failure in this.Failures)
        {
            hash ^= FailureContentHashCode(failure);
        }

        return hash;
    }

    /// <summary>
    /// Compares two <see cref="ValidationFailure"/> instances by their full-fidelity field content rather than
    /// by reference (<see cref="ValidationFailure"/> itself has no value equality of its own).
    /// </summary>
    private static bool FailureContentEquals(ValidationFailure left, ValidationFailure right) =>
        left.PropertyName == right.PropertyName
        && left.ErrorMessage == right.ErrorMessage
        && left.Severity == right.Severity
        && left.ErrorCode == right.ErrorCode
        && Equals(left.CustomState, right.CustomState)
        && Equals(left.AttemptedValue, right.AttemptedValue);

    /// <summary>
    /// Computes a hash code from the same fields <see cref="FailureContentEquals"/> compares, so the two stay
    /// consistent.
    /// </summary>
    private static int FailureContentHashCode(ValidationFailure failure) =>
        HashCode.Combine(
            failure.PropertyName,
            failure.ErrorMessage,
            failure.Severity,
            failure.ErrorCode,
            failure.CustomState,
            failure.AttemptedValue
        );
}
