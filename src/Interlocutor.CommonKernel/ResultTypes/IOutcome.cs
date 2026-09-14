namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a type as a result union representing an operation's outcome.
/// </summary>
/// <remarks>
/// <para>
/// Adds no members of its own - purely an opt-in classification marker. A union that both declares
/// <c>: IOutcome</c> and is attributed with <see cref="GenerateOutcomeMembersAttribute"/> additionally gets
/// <see cref="IOutcomeCase"/> and its <see cref="IOutcomeCase.ActiveCaseType"/> member; a union that skips this
/// interface is a plain tagged union with no success/failure opinion, and never enters the reflective
/// case-resolution fallback <see cref="IOutcomeCase"/> exists to support.
/// </para>
/// </remarks>
public interface IOutcome;
