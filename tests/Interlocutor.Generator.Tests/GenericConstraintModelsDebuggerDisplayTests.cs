namespace Interlocutor.Generator.Tests;

using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>[DebuggerDisplay]</c> expressions added to the generic-constraint and open-generic
/// resolution models only reference members that actually exist (ticket 04). These models are all
/// <see langword="internal"/>, so each is looked up by name via reflection rather than referenced directly.
/// </summary>
public class GenericConstraintModelsDebuggerDisplayTests
{
    private static readonly System.Reflection.Assembly GeneratorAssembly = typeof(OutcomeMemberGenerator).Assembly;

    [Fact]
    public void OpenGenericHandlerModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.OpenGenericHandlerModel", throwOnError: true)!
        );

    [Fact]
    public void ClosedGenericRequestUsageModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.ClosedGenericRequestUsageModel", throwOnError: true)!
        );

    [Fact]
    public void GenericConstraintSnapshot_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.GenericConstraintSnapshot", throwOnError: true)!
        );

    [Fact]
    public void GenericParameterConstraintModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.GenericParameterConstraintModel", throwOnError: true)!
        );

    [Fact]
    public void ConstraintConversionFact_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.ConstraintConversionFact", throwOnError: true)!
        );

    [Fact]
    public void ResolvedArgumentTypeFact_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.ResolvedArgumentTypeFact", throwOnError: true)!
        );

    [Fact]
    public void TypeAnalysisResult_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.TypeAnalysisResult", throwOnError: true)!
        );
}
