namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled, non-union <see cref="IOutcome"/> implementation - standing in for a consumer
/// type that has no compiler-generated case constructor and no <c>IUnion</c> marker, so the generic
/// catch-all overloads on <see cref="ResultExtensions"/> can only handle it via a custom
/// <see cref="Interlocutor.Abstractions.Pipeline.Cases.IOutcomeCaseRegistry"/> registration.
/// </summary>
public sealed class CustomOutcome : IOutcome
{
    private CustomOutcome(Type caseType, object? caseValue)
    {
        this.CaseType = caseType;
        this.CaseValue = caseValue;
    }

    /// <summary>Gets the runtime type of the active case.</summary>
    /// <value>The runtime type of the active case.</value>
    public Type CaseType { get; }

    /// <summary>Gets the active case value.</summary>
    /// <value>The active case value.</value>
    public object? CaseValue { get; }

    /// <summary>Creates a <see cref="CustomOutcome"/> carrying a success value.</summary>
    /// <remarks>
    /// Uses the built-in <see cref="Success"/> marker rather than an arbitrary payload type deliberately:
    /// <see cref="ResultExtensions"/>'s catch-all overloads classify success/failure via
    /// <see cref="Interlocutor.Abstractions.Pipeline.Classifiers.OutcomeClassifierBuilder.DefaultClassifier"/>,
    /// which is fail-closed for any case type it doesn't recognize - <see cref="Success"/> is pre-registered,
    /// an arbitrary payload type would not be. A consumer whose non-union outcome carries a domain success
    /// payload rather than the bare marker needs that payload type registered with a custom classifier for
    /// the catch-all overloads to classify it correctly.
    /// </remarks>
    /// <param name="value">The success value.</param>
    /// <returns>A <see cref="CustomOutcome"/> whose active case is <paramref name="value"/>.</returns>
    public static CustomOutcome FromSuccess(Success value) => new(typeof(Success), value);

    /// <summary>Creates a <see cref="CustomOutcome"/> carrying a failure value.</summary>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="failure">The failure value.</param>
    /// <returns>A <see cref="CustomOutcome"/> whose active case is <paramref name="failure"/>.</returns>
    public static CustomOutcome FromFailure<TFailure>(TFailure failure)
        where TFailure : notnull => new(typeof(TFailure), failure);
}
