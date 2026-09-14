namespace Interlocutor.CommonKernel.ResultTypes;

using System;

/// <summary>
/// Implemented by every compiler-generated <c>union</c> that both is attributed with
/// <see cref="GenerateOutcomeMembersAttribute"/> and declares <see cref="IOutcome"/> in its own base list - the
/// attribute emits this member only for such unions, leaving one declared without <see cref="IOutcome"/> a
/// plain tagged union with no case-identity surface. Exposes the active case's identity directly, so a
/// reflection-based consumer (the outcome classifier, the outcome case registry, AspNetCore's HTTP mapping)
/// doesn't have to infer it from <see cref="System.Runtime.CompilerServices.IUnion.Value"/>'s runtime type -
/// which is <see langword="null"/> both when a case legitimately holds a <see langword="null"/> payload (a
/// nullable reference type, or a <see cref="Nullable{T}"/> with no value) and when the union was never assigned
/// a case at all, and so can't tell the two apart on its own.
/// </summary>
public interface IOutcomeCase
{
    /// <summary>
    /// Gets the active case's declared type, or <see langword="null"/> if no case was ever set.
    /// </summary>
    /// <value>
    /// The declared case type - resolved even when <see cref="System.Runtime.CompilerServices.IUnion.Value"/>
    /// is itself <see langword="null"/>, as long as exactly the active case's own type accepts a
    /// <see langword="null"/> value. <see langword="null"/> only for a union that was never constructed
    /// from any case (e.g. <see langword="default"/>).
    /// </value>
    Type? ActiveCaseType { get; }
}
