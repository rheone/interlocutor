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
[DebuggerDisplay("{DisplayName}")]
internal sealed record OutcomeUnionModel(
    string Namespace,
    string ModifiersText,
    string TypeName,
    ImmutableArray<string> TypeParameterNames,
    ImmutableArray<string> CaseTypeDisplayStrings,
    string DisplayName,
    string HintName,
    Location Location
);
