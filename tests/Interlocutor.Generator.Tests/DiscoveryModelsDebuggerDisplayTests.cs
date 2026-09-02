namespace Interlocutor.Generator.Tests;

using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>[DebuggerDisplay]</c> expressions added to the source generator's handler/request/outcome-
/// union discovery models only reference members that actually exist (ticket 03). These models are all
/// <see langword="internal"/>, so each is looked up by name via reflection rather than referenced directly.
/// </summary>
public class DiscoveryModelsDebuggerDisplayTests
{
    private static readonly System.Reflection.Assembly GeneratorAssembly = typeof(OutcomeMemberGenerator).Assembly;

    [Fact]
    public void HandlerModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.HandlerModel", throwOnError: true)!
        );

    [Fact]
    public void RequestModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.RequestModel", throwOnError: true)!
        );

    [Fact]
    public void OutcomeUnionModel_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(
            GeneratorAssembly.GetType("Interlocutor.Generator.OutcomeUnionModel", throwOnError: true)!
        );
}
