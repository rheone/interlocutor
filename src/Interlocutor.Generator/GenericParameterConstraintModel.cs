namespace Interlocutor.Generator;

using System.Collections.Immutable;

/// <summary>
/// An equatable snapshot of one open generic handler's own type parameter's constraints, captured at build time
/// so <see cref="OpenGenericHandlerResolver"/> can decide whether a discovered concrete type argument satisfies
/// them without re-resolving the handler's declaration.
/// </summary>
/// <param name="ParameterName">
/// The type parameter's own declared name (e.g. <c>"T"</c>) - used to find the concrete argument bound to it
/// when a handler closes over a discovered request usage (see <see cref="OpenGenericHandlerModel.RequestArgumentParameterNames"/>).
/// </param>
/// <param name="HasReferenceTypeConstraint">Whether the parameter declares a <c>class</c> constraint.</param>
/// <param name="HasValueTypeConstraint">Whether the parameter declares a <c>struct</c> constraint.</param>
/// <param name="HasConstructorConstraint">Whether the parameter declares a <c>new()</c> constraint.</param>
/// <param name="HasNotNullConstraint">Whether the parameter declares a <c>notnull</c> constraint.</param>
/// <param name="ConstraintTypeMetadataNames">
/// The CLR metadata name (resolvable via <see cref="Microsoft.CodeAnalysis.Compilation.GetTypeByMetadataName(string)"/>,
/// no <c>global::</c> prefix) of every base type/interface constraint, or <see langword="null"/> for a
/// constraint type this generator cannot resolve that way (a generic constraint type) - such an entry is
/// treated as satisfied rather than rejected, since it cannot be soundly checked from a metadata name alone.
/// </param>
internal sealed record GenericParameterConstraintModel(
    string ParameterName,
    bool HasReferenceTypeConstraint,
    bool HasValueTypeConstraint,
    bool HasConstructorConstraint,
    bool HasNotNullConstraint,
    ImmutableArray<string?> ConstraintTypeMetadataNames
);
