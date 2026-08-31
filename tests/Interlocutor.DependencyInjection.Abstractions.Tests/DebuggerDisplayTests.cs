namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>[DebuggerDisplay]</c> expressions added to <see cref="RegistrationReport"/> and its
/// registration-entry types only reference members that actually exist (ticket 02).
/// </summary>
public class DebuggerDisplayTests
{
    [Fact]
    public void RegistrationReport_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(typeof(RegistrationReport));

    [Fact]
    public void InterlocutorRegistration_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(typeof(InterlocutorRegistration));

    [Fact]
    public void InterlocutorFactoryRegistration_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(typeof(InterlocutorFactoryRegistration));
}
