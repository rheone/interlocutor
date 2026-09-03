namespace Interlocutor.Generator.Models;

using System.Diagnostics;
using Interlocutor.Generator.Analysis;

/// <summary>
/// Whether a conversion from one concrete argument type to one open generic handler constraint type is an
/// identity or implicit conversion - the only conversions <see cref="GenericConstraintChecker.Satisfies"/>
/// accepts - see <see cref="GenericConstraintSnapshot.ConversionFacts"/>.
/// </summary>
/// <param name="ArgumentMetadataName">The concrete type argument's CLR metadata name.</param>
/// <param name="ConstraintTypeMetadataName">The open generic handler constraint's own CLR metadata name.</param>
/// <param name="IsSatisfyingConversion">
/// Whether <see cref="Microsoft.CodeAnalysis.CSharp.CSharpCompilation.ClassifyConversion(Microsoft.CodeAnalysis.ITypeSymbol, Microsoft.CodeAnalysis.ITypeSymbol)"/>
/// found an identity or implicit conversion from the argument type to the constraint type.
/// </param>
[DebuggerDisplay("{ArgumentMetadataName} -> {ConstraintTypeMetadataName}: {IsSatisfyingConversion}")]
internal readonly record struct ConstraintConversionFact(
    string ArgumentMetadataName,
    string ConstraintTypeMetadataName,
    bool IsSatisfyingConversion
);
