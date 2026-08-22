namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;

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
/// <param name="HasNotNullConstraint">
/// Whether the parameter declares a <c>notnull</c> constraint. Captured for completeness but never enforced by
/// <see cref="GenericConstraintChecker.Satisfies"/> - see that method's remarks for why a nullable-annotation
/// constraint cannot be soundly checked from a metadata name alone.
/// </param>
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
)
{
    /// <inheritdoc/>
    /// <remarks>
    /// Overridden for the same reason as <see cref="CompilationInfo.Equals(CompilationInfo?)"/>:
    /// <see cref="ImmutableArray{T}"/>'s own <c>Equals</c> compares its backing array by reference, not by
    /// element, so the compiler-generated positional-record equality this type would otherwise get from
    /// <see cref="ConstraintTypeMetadataNames"/> is useless for incremental-pipeline caching (ticket 53).
    /// </remarks>
    public bool Equals(GenericParameterConstraintModel? other) =>
        other is not null
        && this.ParameterName == other.ParameterName
        && this.HasReferenceTypeConstraint == other.HasReferenceTypeConstraint
        && this.HasValueTypeConstraint == other.HasValueTypeConstraint
        && this.HasConstructorConstraint == other.HasConstructorConstraint
        && this.HasNotNullConstraint == other.HasNotNullConstraint
        && this.ConstraintTypeMetadataNames.SequenceEqual(other.ConstraintTypeMetadataNames);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method.
        var hash = 17;
        hash = (hash * 31) + this.ParameterName.GetHashCode();
        hash = (hash * 31) + this.HasReferenceTypeConstraint.GetHashCode();
        hash = (hash * 31) + this.HasValueTypeConstraint.GetHashCode();
        hash = (hash * 31) + this.HasConstructorConstraint.GetHashCode();
        hash = (hash * 31) + this.HasNotNullConstraint.GetHashCode();
        foreach (var name in this.ConstraintTypeMetadataNames)
        {
            hash = (hash * 31) + (name?.GetHashCode() ?? 0);
        }

        return hash;
    }
}
