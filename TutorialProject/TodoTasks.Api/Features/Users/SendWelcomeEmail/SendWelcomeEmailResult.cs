namespace TodoTasks.Api.Features.Users.SendWelcomeEmail;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// The hand-rolled <see cref="IUnion"/> response <see cref="SendWelcomeEmailHandler"/> returns -
/// Interlocutor's own worked example, inside a real feature, of a response type that implements
/// <see cref="IUnion"/> directly rather than being one of this project's usual compiler-generated
/// <c>Outcome&lt;...&gt;</c> unions. See <c>documentation/messages-and-result-types.md</c>'s "IUnion as
/// an Extension Point" section for the full explanation this type makes concrete, and
/// <see cref="SendWelcomeEmailResultCaseRegistry"/> for the companion piece that teaches
/// <c>IOutcomeCaseRegistry</c> to construct a case of this type.
/// </summary>
/// <remarks>
/// <para>
/// This command is not gated by <c>IRequiresPolicyRequest</c> and carries no FluentValidation validator,
/// so none of this project's pipeline behaviors ever need to construct a case of this type at dispatch
/// time - <see cref="SendWelcomeEmailHandler"/> is the only place <see cref="FromSuccess"/>/
/// <see cref="FromFailure{TFailure}"/> are called in production code. The registered
/// <see cref="SendWelcomeEmailResultCaseRegistry"/> factories exist to demonstrate, and prove by test,
/// that construction works for a hand-rolled type like this one - the same seam a marker-gated behavior
/// would go through for a request type that did need it.
/// </para>
/// <para>
/// <see cref="FromSuccess"/> takes the bare <see cref="Success"/> marker, not a custom payload type,
/// deliberately - <c>documentation/aspnetcore-integration.md</c> is explicit that
/// <c>ToMinimalApiResult()</c>'s reflective catch-all overload (the one a hand-rolled, non-<c>Outcome</c>
/// response type like this one requires) can only classify a resolved, non-<see langword="null"/> active
/// case as success when it's the bare <see cref="Success"/> marker; any other payload type throws
/// <see cref="Interlocutor.AspNetCore.OutcomeCaseNotMappableException"/> rather than guessing.
/// </para>
/// </remarks>
public sealed class SendWelcomeEmailResult : IUnion
{
    private SendWelcomeEmailResult(Type caseType, object? caseValue)
    {
        this.CaseType = caseType;
        this.CaseValue = caseValue;
    }

    /// <summary>Gets the active case's runtime type.</summary>
    /// <value>Either <c>typeof(Success)</c> or the runtime type of whichever failure case is active.</value>
    public Type CaseType { get; }

    /// <summary>Gets the active case's value.</summary>
    /// <value>A <see cref="Success"/> on success, or the active failure case value.</value>
    public object? CaseValue { get; }

    /// <inheritdoc/>
    object? IUnion.Value => this.CaseValue;

    /// <summary>Creates a <see cref="SendWelcomeEmailResult"/> whose active case is <see cref="Success"/>.</summary>
    /// <param name="value">The success marker to carry.</param>
    /// <returns>The constructed result.</returns>
    public static SendWelcomeEmailResult FromSuccess(Success value) => new(typeof(Success), value);

    /// <summary>Creates a <see cref="SendWelcomeEmailResult"/> whose active case is a failure.</summary>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="failure">The failure value to carry.</param>
    /// <returns>The constructed result.</returns>
    public static SendWelcomeEmailResult FromFailure<TFailure>(TFailure failure)
        where TFailure : notnull => new(typeof(TFailure), failure);
}
