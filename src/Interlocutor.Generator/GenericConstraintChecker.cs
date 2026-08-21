namespace Interlocutor.Generator;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Decides whether a concrete type argument satisfies one of an open generic handler's own type parameter
/// constraints (ADR-0008, ticket 11) - the check that keeps a request whose type argument fails a handler's
/// <c>where</c> clause from being closed over that handler.
/// </summary>
/// <remarks>
/// Resolution happens against the live <see cref="Compilation"/> at source-production time, not at analysis
/// time, because the constraint and the concrete argument are usually discovered independently, from different
/// syntax trees (see <see cref="OpenGenericHandlerModel"/> and <see cref="ClosedGenericRequestUsageModel"/>'s
/// remarks) - both are re-resolved here from the metadata names each side already captured.
/// </remarks>
internal static class GenericConstraintChecker
{
    /// <summary>
    /// Whether <paramref name="argumentMetadataName"/> satisfies every clause of <paramref name="constraint"/>.
    /// </summary>
    /// <param name="constraint">The open generic handler's type parameter constraint to check against.</param>
    /// <param name="argumentMetadataName">
    /// The concrete type argument's CLR metadata name, or <see langword="null"/> if this generator could not
    /// resolve one for it (a constructed generic, array, or similar shape) - treated as satisfying every
    /// constraint, since it cannot be soundly checked from a metadata name alone.
    /// </param>
    /// <param name="compilation">The compilation to re-resolve both sides' metadata names against.</param>
    /// <returns><see langword="true"/> if every constraint clause is satisfied (or unresolvable and therefore assumed satisfied).</returns>
    public static bool Satisfies(GenericParameterConstraintModel constraint, string? argumentMetadataName, Compilation compilation)
    {
        if (argumentMetadataName is null)
        {
            return true;
        }

        var argumentType = compilation.GetTypeByMetadataName(argumentMetadataName);
        if (argumentType is null)
        {
            return true;
        }

        if (constraint.HasValueTypeConstraint && !argumentType.IsValueType)
        {
            return false;
        }

        if (constraint.HasReferenceTypeConstraint && !argumentType.IsReferenceType)
        {
            return false;
        }

        if (constraint.HasConstructorConstraint && !HasAccessibleParameterlessConstructor(argumentType))
        {
            return false;
        }

        foreach (var constraintTypeName in constraint.ConstraintTypeMetadataNames)
        {
            if (constraintTypeName is null)
            {
                // Unresolvable constraint type (itself generic, most commonly) - cannot be soundly checked from
                // a metadata name, so it is treated as satisfied rather than rejected.
                continue;
            }

            var constraintType = compilation.GetTypeByMetadataName(constraintTypeName);
            if (constraintType is null)
            {
                continue;
            }

            // ClassifyConversion is a CSharpCompilation member, not one on the language-agnostic base type -
            // this generator is C#-only (see InterlocutorSourceGenerator's [Generator(LanguageNames.CSharp)]),
            // so the cast always succeeds in practice.
            var conversion = ((CSharpCompilation)compilation).ClassifyConversion(argumentType, constraintType);
            if (!conversion.Exists || !(conversion.IsIdentity || conversion.IsImplicit))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether <paramref name="type"/> has a public, accessible, parameterless constructor - or is a value type, which always does.</summary>
    private static bool HasAccessibleParameterlessConstructor(ITypeSymbol type) =>
        type.IsValueType
        || (
            type is INamedTypeSymbol named
            && named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)
        );
}
