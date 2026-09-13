namespace Interlocutor.Generator.Analysis;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Builds a <see cref="GenericConstraintSnapshot"/> from the live compilation, exactly once per compilation
/// (ticket 53) - the only place in the incremental pipeline that still calls
/// <see cref="Compilation.GetTypeByMetadataName(string)"/> or
/// <see cref="CSharpCompilation.ClassifyConversion(ITypeSymbol, ITypeSymbol)"/> for open generic handler
/// resolution, so that <see cref="OpenGenericHandlerResolver"/> and <see cref="GenericConstraintChecker"/>
/// downstream never need the raw <see cref="Compilation"/> object themselves.
/// </summary>
internal static class GenericConstraintSnapshotBuilder
{
    /// <summary>
    /// Resolves every distinct concrete type argument and constraint type metadata name found across
    /// <paramref name="openHandlers"/> and <paramref name="usages"/> against <paramref name="compilation"/>,
    /// once, and captures the facts <see cref="GenericConstraintChecker.Satisfies"/> needs from each.
    /// </summary>
    /// <param name="compilation">The compilation to resolve metadata names against.</param>
    /// <param name="openHandlers">Every open generic query/command handler discovered in the compilation.</param>
    /// <param name="usages">Every fully closed generic type usage found anywhere in the compilation.</param>
    /// <returns>The resulting snapshot.</returns>
    public static GenericConstraintSnapshot Build(
        Compilation compilation,
        ImmutableArray<OpenGenericHandlerModel> openHandlers,
        ImmutableArray<ClosedGenericRequestUsageModel> usages
    )
    {
        var argumentMetadataNames = usages
            .SelectMany(static u => u.TypeArgumentMetadataNames)
            .Where(static n => n is not null)
            .Select(static n => n!)
            .Distinct(System.StringComparer.Ordinal)
            .ToImmutableArray();

        var constraintTypeMetadataNames = openHandlers
            .SelectMany(static h => h.Constraints)
            .SelectMany(static c => c.ConstraintTypeMetadataNames)
            .Where(static n => n is not null)
            .Select(static n => n!)
            .Distinct(System.StringComparer.Ordinal)
            .ToImmutableArray();

        var resolvedArgumentTypes = new Dictionary<string, ITypeSymbol?>(System.StringComparer.Ordinal);
        var argumentFacts = ImmutableArray.CreateBuilder<ResolvedArgumentTypeFact>();
        foreach (var name in argumentMetadataNames)
        {
            var type = compilation.GetTypeByMetadataName(name);
            resolvedArgumentTypes[name] = type;
            if (type is not null)
            {
                argumentFacts.Add(
                    new ResolvedArgumentTypeFact(
                        name,
                        type.IsValueType,
                        type.IsReferenceType,
                        HasAccessibleParameterlessConstructor(type)
                    )
                );
            }
        }

        var resolvedConstraintTypes = new Dictionary<string, ITypeSymbol?>(System.StringComparer.Ordinal);
        foreach (var name in constraintTypeMetadataNames)
        {
            resolvedConstraintTypes[name] = compilation.GetTypeByMetadataName(name);
        }

        var conversionFacts = ImmutableArray.CreateBuilder<ConstraintConversionFact>();
        foreach (var argumentName in argumentMetadataNames)
        {
            var argumentType = resolvedArgumentTypes[argumentName];
            if (argumentType is null)
            {
                continue;
            }

            foreach (var constraintTypeName in constraintTypeMetadataNames)
            {
                var constraintType = resolvedConstraintTypes[constraintTypeName];
                if (constraintType is null)
                {
                    continue;
                }

                // ClassifyConversion is a CSharpCompilation member, not one on the language-agnostic base
                // type - this generator is C#-only (see InterlocutorSourceGenerator's [Generator(LanguageNames.CSharp)]),
                // so the cast always succeeds in practice.
                var conversion = ((CSharpCompilation)compilation).ClassifyConversion(argumentType, constraintType);
                var isSatisfying = conversion.Exists && (conversion.IsIdentity || conversion.IsImplicit);
                conversionFacts.Add(new ConstraintConversionFact(argumentName, constraintTypeName, isSatisfying));
            }
        }

        return new GenericConstraintSnapshot(argumentFacts.ToImmutable(), conversionFacts.ToImmutable());
    }

    /// <summary>Whether <paramref name="type"/> has a public, accessible, parameterless constructor - or is a value type, which always does.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> is a value type or declares a public parameterless constructor.</returns>
    private static bool HasAccessibleParameterlessConstructor(ITypeSymbol type) =>
        type.IsValueType
        || (
            type is INamedTypeSymbol named
            && named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)
        );
}
