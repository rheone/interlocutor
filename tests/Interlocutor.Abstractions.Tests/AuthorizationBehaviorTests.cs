namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

// AuthorizationBehavior<TRequest, TSuccess> now constrains TRequest : IRequiresPolicy, so a request that
// doesn't require a policy cannot close this generic at all — there is no runtime "skip" branch left to unit
// test here. The "a request without the marker never resolves the behavior" guarantee is proven at the
// container level instead: see BehaviorSubsetConstraintTests in the DependencyInjection.Microsoft/Autofac
// test projects.
public class AuthorizationBehaviorTests
{
    public record ProtectedQuery(string Name) : IQuery<OperationOutcome<string, Forbidden>>, IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    [Fact]
    public async Task Handle_AllowsAccessWhenPolicyEvaluatesTrue()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result.Value);
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ReturnsForbidenWhenPolicyEvaluatesFalse()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            throw new InvalidOperationException("Handler should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Should return Forbidden instead of calling next
        Assert.IsType<Forbidden>(result.Value);
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_IncludesPolicyNameInForbiddenReason()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Contains("CanRead", forbidden.Reason ?? string.Empty);
    }

    [Fact]
    public async Task Handle_DoesNotCallNextWhenAuthorizationFails()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        var nextCalled = false;

        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert - next should not be called
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_PassesRequestToPolicyEvaluator()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test-value");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () =>
            Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Request should be passed to evaluator
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is<object>(r => r == request), CancellationToken.None);
    }
}
