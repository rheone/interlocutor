namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="DelegatePipelineBehavior{TRequest, TResponse}"/>.</summary>
public class DelegatePipelineBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WithBothDelegatesSupplied_InvokesBothAroundNext()
    {
        // Arrange
        var callOrder = new List<string>();
        var handlerResult = new Outcome<string, Failure>("handled");
        var replacement = new Outcome<string, Failure>("replaced");
        var behavior = new DelegatePipelineBehavior<TestRequest, Outcome<string, Failure>>(
            (_, _) =>
            {
                callOrder.Add("Enter");
                return Task.FromResult(StageResult<Outcome<string, Failure>>.Continue);
            },
            (_, result, _) =>
            {
                callOrder.Add("Exit");
                Assert.Equal(handlerResult, result);
                return Task.FromResult(replacement);
            }
        );

        // Act
        var result = await behavior.HandleAsync(
            new TestRequest(),
            async () =>
            {
                callOrder.Add("Next");
                return handlerResult;
            },
            CancellationToken.None
        );

        // Assert
        Assert.Equal(["Enter", "Next", "Exit"], callOrder);
        Assert.Equal(replacement, result);
    }

    [Fact]
    public async Task HandleAsync_WithOnEnterOmitted_FallsBackToBaseContinueAndNextStillRuns()
    {
        // Arrange
        var nextRan = false;
        var handlerResult = new Outcome<string, Failure>("handled");
        var behavior = new DelegatePipelineBehavior<TestRequest, Outcome<string, Failure>>(
            onEnter: null,
            (_, result, _) => Task.FromResult(result)
        );

        // Act
        var result = await behavior.HandleAsync(
            new TestRequest(),
            async () =>
            {
                nextRan = true;
                return handlerResult;
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(nextRan);
        Assert.Equal(handlerResult, result);
    }

    [Fact]
    public async Task HandleAsync_WithOnExitOmitted_FallsBackToBasePassThrough()
    {
        // Arrange
        var handlerResult = new Outcome<string, Failure>("handled");
        var behavior = new DelegatePipelineBehavior<TestRequest, Outcome<string, Failure>>(
            (_, _) => Task.FromResult(StageResult<Outcome<string, Failure>>.Continue),
            onExit: null
        );

        // Act
        var result = await behavior.HandleAsync(new TestRequest(), async () => handlerResult, CancellationToken.None);

        // Assert: no exit delegate supplied, so the handler's result passes through unchanged.
        Assert.Equal(handlerResult, result);
    }

    [Fact]
    public async Task HandleAsync_WithBothDelegatesOmitted_IsAPurePassThrough()
    {
        // Arrange
        var handlerResult = new Outcome<string, Failure>("handled");
        var behavior = new DelegatePipelineBehavior<TestRequest, Outcome<string, Failure>>(onEnter: null, onExit: null);
        var nextRan = false;

        // Act
        var result = await behavior.HandleAsync(
            new TestRequest(),
            async () =>
            {
                nextRan = true;
                return handlerResult;
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(nextRan);
        Assert.Equal(handlerResult, result);
    }

    // Deliberately empty: a type-identity marker for this test's generic-constraint shape, not a data holder.
#pragma warning disable S2094
    public record TestRequest;
#pragma warning restore S2094
}
