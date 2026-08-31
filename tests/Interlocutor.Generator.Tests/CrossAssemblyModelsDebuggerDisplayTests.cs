namespace Interlocutor.Generator.Tests;

using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>[DebuggerDisplay]</c> expressions added to the cross-assembly module discovery models only
/// reference members that actually exist (ticket 05). These models are all <see langword="internal"/>, so each
/// is looked up by name via reflection rather than referenced directly.
/// </summary>
public class CrossAssemblyModelsDebuggerDisplayTests
{
    private static readonly System.Reflection.Assembly GeneratorAssembly = typeof(OutcomeMemberGenerator).Assembly;

    [Fact]
    public void ExternalModuleInfo_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.ExternalModuleInfo", throwOnError: true)!
        );

    [Fact]
    public void ExternalHandlerInfo_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.ExternalHandlerInfo", throwOnError: true)!
        );

    [Fact]
    public void CompilationInfo_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.CompilationInfo", throwOnError: true)!
        );
}
