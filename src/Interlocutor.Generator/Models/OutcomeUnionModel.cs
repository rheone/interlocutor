namespace Interlocutor.Generator.Models;

using System.Collections.Immutable;
using System.Diagnostics;
using Interlocutor.Generator.Emission;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one <c>partial union</c> declaration attributed with
/// <c>GenerateOutcomeMembersAttribute</c>, holding everything <see cref="OutcomeMemberEmitter"/> needs to emit
/// its generated members.
/// </summary>
/// <param name="Namespace">The declaring namespace, or empty for the global namespace.</param>
/// <param name="ModifiersText">
/// The union declaration's own modifier tokens verbatim (e.g. <c>"public readonly partial"</c>) - a readonly
/// struct's partial declarations must each restate <c>readonly</c>, so the generated part reuses these rather
/// than guessing a subset.
/// </param>
/// <param name="TypeName">The union's simple (unqualified) type name.</param>
/// <param name="TypeParameterNames">
/// The union's own generic type parameter names in declared order, empty for a non-generic union - reused
/// verbatim so the generated partial declaration's type parameter list matches exactly (required for a
/// partial generic type's declarations to agree).
/// </param>
/// <param name="CaseTypeDisplayStrings">
/// Every case type's fully-qualified display string, in declared order - the first is the success case, every
/// remaining entry an independent failure case. For a generic case type parameter this is just its name (a
/// type parameter is never qualified); for a concrete case type it is <c>global::</c>-prefixed, so the
/// generated file never depends on the original declaration's own <c>using</c> directives.
/// </param>
/// <param name="DisplayName">The union's fully-qualified display name, for diagnostic messages.</param>
/// <param name="HintName">The unique hint name <see cref="OutcomeMemberGenerator"/> passes to <c>AddSource</c>.</param>
/// <param name="Location">The union declaration's identifier location, for diagnostic anchoring.</param>
/// <param name="ImplementsIOutcome">
/// Whether the union's own declaration lists <c>IOutcome</c> in its base list (resolved semantically against
/// <c>Interlocutor.CommonKernel.ResultTypes.IOutcome</c>, never by matching source text). Gates the
/// classification surface - <c>IOutcomeCase</c>, <c>IsSuccess</c>, <c>ActiveCaseType</c> - which
/// <see cref="OutcomeMemberEmitter"/> only emits when this is <see langword="true"/>. The sum-type members
/// (<c>Match</c>, <c>Switch</c>, per-case <c>TryGetValue</c>, <c>ToString</c>, equality) are unconditional -
/// a union declared without <c>: IOutcome</c> is a plain tagged union with no success/failure opinion, not a
/// lesser outcome.
/// </param>
/// <param name="CaseAccessorNames">
/// Parallel to <see cref="CaseTypeDisplayStrings"/>: each concrete case type's simple name (e.g.
/// <c>"NotFound"</c>, or <c>"ValidationError"</c> for a closed <c>ValidationError&lt;string&gt;</c> - generic
/// arguments never appear in the accessor name), or <see langword="null"/> when that case type is one of the
/// union's own declared type parameters. <see cref="OutcomeMemberEmitter"/> emits a named
/// <c>TryGet{Name}(out {Type} value)</c> accessor, additive alongside the anonymous <c>TryGetValue</c>
/// overload, for every non-null entry - a type parameter's name carries no case-specific meaning (every case
/// type at the shipped <c>Outcome&lt;TSuccessResult, TCase2, ...&gt;</c> family's declaration site *is* a type
/// parameter), so those stay reachable only through the anonymous, type-inferred overload.
/// </param>
[DebuggerDisplay("{DisplayName}")]
internal sealed record OutcomeUnionModel(
    string Namespace,
    string ModifiersText,
    string TypeName,
    ImmutableArray<string> TypeParameterNames,
    ImmutableArray<string> CaseTypeDisplayStrings,
    string DisplayName,
    string HintName,
    Location Location,
    bool ImplementsIOutcome,
    ImmutableArray<string?> CaseAccessorNames
);
