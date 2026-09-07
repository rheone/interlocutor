namespace Interlocutor.Generator.Tests.Models;

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
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.OpenGenericHandlerModel", throwOnError: true)!
        );

    [Fact]
    public void ClosedGenericRequestUsageModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.ClosedGenericRequestUsageModel", throwOnError: true)!
        );

    [Fact]
    public void GenericConstraintSnapshot_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.GenericConstraintSnapshot", throwOnError: true)!
        );

    [Fact]
    public void GenericParameterConstraintModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.GenericParameterConstraintModel", throwOnError: true)!
        );

    [Fact]
    public void ConstraintConversionFact_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.ConstraintConversionFact", throwOnError: true)!
        );

    [Fact]
    public void ResolvedArgumentTypeFact_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.ResolvedArgumentTypeFact", throwOnError: true)!
        );

    [Fact]
    public void TypeAnalysisResult_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.Models.TypeAnalysisResult", throwOnError: true)!
        );
}
