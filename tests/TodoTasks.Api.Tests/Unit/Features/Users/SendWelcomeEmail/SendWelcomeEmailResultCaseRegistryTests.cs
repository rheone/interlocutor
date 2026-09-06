namespace TodoTasks.Api.Tests.Unit.Features.Users.SendWelcomeEmail;

using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Features.Users.SendWelcomeEmail;

/// <summary>
/// Proves <see cref="SendWelcomeEmailResultCaseRegistry"/>'s registered factories actually construct a
/// <see cref="SendWelcomeEmailResult"/>, and that its active case round-trips back out through
/// <see cref="IOutcomeCaseRegistry.TryGetActiveCase{TResponse}"/> unchanged.
/// </summary>
/// <remarks>
/// <see cref="SendWelcomeEmailCommand"/> implements neither <c>IRequiresPolicyRequest</c> nor carries a
/// FluentValidation validator, so none of <c>TodoTasks.Api</c>'s pipeline behaviors
/// (<c>AuthorizationBehavior</c>, <c>ValidationBehavior</c>) ever call
/// <see cref="IOutcomeCaseRegistry.Construct{TResponse,TCase}"/> against
/// <see cref="SendWelcomeEmailResult"/> during a real dispatch - there is no marker-gated behavior in
/// this project's pipeline for this command to exercise that construction path end to end. These tests
/// exercise <see cref="SendWelcomeEmailResultCaseRegistry"/> directly instead, proving the registration
/// <c>Program.cs</c> wires up via <c>OverrideOutcomeCaseRegistry</c> is itself correct - the same seam a
/// future marker-gated behavior on this command would rely on.
/// </remarks>
public sealed class SendWelcomeEmailResultCaseRegistryTests
{
    private readonly IOutcomeCaseRegistry registry = new SendWelcomeEmailResultCaseRegistry();

    [Fact]
    public void Construct_ANotFoundCase_BuildsAResultWhoseActiveCaseRoundTrips_Test()
    {
        // Arrange
        var notFound = new NotFound("User 42");

        // Act
        var result = this.registry.Construct<SendWelcomeEmailResult, NotFound>(notFound);

        // Assert
        Assert.True(this.registry.TryGetActiveCase(result, out var caseType, out var caseValue));
        Assert.Equal(typeof(NotFound), caseType);
        Assert.Equal(notFound, Assert.IsType<NotFound>(caseValue));
    }

    [Fact]
    public void Construct_AFailureCase_BuildsAResultWhoseActiveCaseRoundTrips_Test()
    {
        // Arrange
        var failure = new Failure("not deliverable");

        // Act
        var result = this.registry.Construct<SendWelcomeEmailResult, Failure>(failure);

        // Assert
        Assert.True(this.registry.TryGetActiveCase(result, out var caseType, out var caseValue));
        Assert.Equal(typeof(Failure), caseType);
        Assert.Equal(failure, Assert.IsType<Failure>(caseValue));
    }

    [Fact]
    public void TryGetActiveCase_ASuccessResult_ExtractsItGenerically_WithNoFactoryRegistered_Test()
    {
        // Arrange - SendWelcomeEmailResult.FromSuccess is what SendWelcomeEmailHandler calls directly, not
        // something routed through the registry's Construct method; this proves the *extraction* half
        // (TryGetActiveCase), which the doc explains works generically for any IUnion with no factory
        // needed, still works for this hand-rolled type via the same registry instance.
        var result = SendWelcomeEmailResult.FromSuccess(default);

        // Act
        var found = this.registry.TryGetActiveCase(result, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(Success), caseType);
        Assert.IsType<Success>(caseValue);
    }
}
