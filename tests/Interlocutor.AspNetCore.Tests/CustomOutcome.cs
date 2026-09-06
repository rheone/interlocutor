namespace Interlocutor.AspNetCore.Tests;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A hand-rolled, non-union <see cref="IUnion"/> implementation - standing in for a consumer
/// type that has no compiler-generated case constructor, so the generic catch-all overloads on
/// <see cref="ResultExtensions"/> can extract its active case automatically (any real <see cref="IUnion"/>
/// implementer is enough for that), but can only *construct* it via a custom
/// <see cref="Interlocutor.Abstractions.Pipeline.Cases.IOutcomeCaseRegistry"/> registration, since
/// <see cref="Interlocutor.Abstractions.Pipeline.UnionCaseFactory"/> has no case constructor to find
/// reflectively for it.
/// </summary>
public sealed class CustomOutcome : IUnion
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

    /// <inheritdoc/>
    object? IUnion.Value => this.CaseValue;

    /// <summary>Creates a <see cref="CustomOutcome"/> carrying a success value.</summary>
    /// <remarks>
    /// Uses the built-in <see cref="Success"/> marker rather than an arbitrary payload type deliberately:
    /// <see cref="ResultExtensions"/>'s catch-all overloads classify success/failure against a small set of
    /// recognized shapes - <see cref="Success"/> is one of them, an arbitrary payload type is not, and would
    /// throw <see cref="Interlocutor.AspNetCore.OutcomeCaseNotMappableException"/> rather than being guessed
    /// one way or the other. A consumer whose non-union outcome carries a domain success payload rather than
    /// the bare marker cannot use these catch-all overloads for it - see
    /// <see cref="GenericCatchAllResultExtensionsTests.ToActionResult_CatchAll_HandRolledOutcome_WithUnrecognizedSuccessPayload_ThrowsOutcomeCaseNotMappableException"/>.
    /// </remarks>
    /// <param name="value">The success value.</param>
    /// <returns>A <see cref="CustomOutcome"/> whose active case is <paramref name="value"/>.</returns>
    public static CustomOutcome FromSuccess(Success value) => new(typeof(Success), value);

    /// <summary>Creates a <see cref="CustomOutcome"/> carrying a failure value.</summary>
    /// <remarks>
    /// A <typeparamref name="TFailure"/> outside the recognized failure shapes <see cref="ResultExtensions"/>'s
    /// catch-all overloads know (see <see cref="Interlocutor.AspNetCore.OutcomeCaseNotMappableException"/>) is
    /// indistinguishable from an unrecognized success payload from this type's own perspective - both are just
    /// "some case value" - so despite the name, passing one produces the same throw as
    /// <see cref="FromSuccess"/> would for a non-<see cref="Success"/> value.
    /// </remarks>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="failure">The failure value.</param>
    /// <returns>A <see cref="CustomOutcome"/> whose active case is <paramref name="failure"/>.</returns>
    public static CustomOutcome FromFailure<TFailure>(TFailure failure)
        where TFailure : notnull => new(typeof(TFailure), failure);
}
