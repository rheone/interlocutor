using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
using NSubstitute;

namespace Mediance.Abstractions.Tests;

public class AuthorizationBehaviorTests
{
    public record ProtectedQuery(string Name) : IQuery<string>, IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    public record UnprotectedQuery(string Name) : IQuery<string>;

    [Fact]
    public async Task Handle_AllowsAccessWhenPolicyEvaluatesTrue()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () => Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

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
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () => throw new InvalidOperationException("Handler should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Should return Forbidden instead of calling next
        Assert.IsType<Forbidden>(result.Value);
        await policyEvaluator.Received(1).EvaluateAsync("CanRead", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_PassesThroughWhenRequestDoesNotRequirePolicy()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        var behavior = new AuthorizationBehavior<UnprotectedQuery, string>(policyEvaluator);
        var request = new UnprotectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () => Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Should skip authorization and return success
        Assert.Equal("success", result.Value);
        // Policy evaluator should not be called
        await policyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesPolicyNameInForbiddenReason()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () => throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Contains("CanRead", forbidden.Reason ?? "");
    }

    [Fact]
    public async Task Handle_DoesNotCallNextWhenAuthorizationFails()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
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
        policyEvaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedQuery, string>(policyEvaluator);
        var request = new ProtectedQuery("test-value");
        Func<Task<OperationOutcome<string, Forbidden>>> next = () => Task.FromResult<OperationOutcome<string, Forbidden>>(new OperationOutcome<string, Forbidden>("success"));

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert - Request should be passed to evaluator
        await policyEvaluator.Received(1).EvaluateAsync(
            "CanRead",
            Arg.Is<object>(r => r == request),
            CancellationToken.None
        );
    }
}
