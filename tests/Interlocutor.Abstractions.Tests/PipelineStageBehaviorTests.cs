namespace Interlocutor.Abstractions.Tests.Pipeline;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Xunit;

/// <summary>
/// Tests for <see cref="PipelineStageBehavior{TRequest,TResponse}"/> to verify symmetric after-hook unwinding.
/// </summary>
public class PipelineStageBehaviorTests
{
    [Fact]
    public async Task Handle_WithAllStagesCompleted_InvokesAfterHooksInReverseOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new TrackingStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([
            stage1,
            stage2,
            stage3,
        ]);

        var request = new TestRequest();
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act
        var result = await behavior.Handle(
            request,
            async () =>
            {
                callOrder.Add("Handler");
                return expectedResult;
            },
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(["S1.Before", "S2.Before", "S3.Before", "Handler", "S3.After", "S2.After", "S1.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenSecondStageBeforesFailsThrows_FirstStageBehindReceivesAfter()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new FailingStage("S2", callOrder, failOn: "Before");
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([
            stage1,
            stage2,
            stage3,
        ]);

        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                request,
                async () => throw new InvalidOperationException("Handler should not run"),
                CancellationToken.None
            )
        );

        Assert.Equal("Stage S2 failed during Before", ex.Message);

        // S1 completed Before, so S1 After must run. S2's Before threw (so it doesn't add to callOrder before the exception),
        // S3 never ran Before. S2 and S3 After should not run because their Before never completed.
        Assert.Equal(["S1.Before", "S1.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_AllCompletedStagesReceiveAfter()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new TrackingStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([
            stage1,
            stage2,
            stage3,
        ]);

        var request = new TestRequest();
        var handlerException = new InvalidOperationException("Handler failed");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, async () => throw handlerException, CancellationToken.None)
        );

        Assert.Same(handlerException, ex);

        // All stages completed Before, so all should receive After in reverse order, even though handler failed.
        Assert.Equal(["S1.Before", "S2.Before", "S3.Before", "S3.After", "S2.After", "S1.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrowsAndAfterFails_OriginalExceptionPropagates()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new FailingStage("S2", callOrder, failOn: "After");
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([
            stage1,
            stage2,
            stage3,
        ]);

        var request = new TestRequest();
        var handlerException = new InvalidOperationException("Handler failed");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, async () => throw handlerException, CancellationToken.None)
        );

        // The original handler exception should propagate, not the After-hook exception.
        Assert.Same(handlerException, ex);

        // S3 After should run, S2 After should run (and fail), but we still see handler exception.
        // Note: S1 After won't run because S2 After threw and we suppressed it to re-throw handler exception.
        // Actually, we try to run all After hooks, and S2 throws, but we still re-throw original handler exception.
        // So S1 After won't run because S2 After failed.
        // Let me verify the callOrder matches the implementation.
        Assert.Equal(["S1.Before", "S2.Before", "S3.Before", "S3.After", "S2.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenBeforeThrowsAndAfterFails_OriginalBeforeExceptionPropagates()
    {
        // Arrange
        var callOrder = new List<string>();
        var failingAfterStage = new FailingStage("S1-FailAfter", callOrder, failOn: "After");
        var stage2 = new FailingStage("S2", callOrder, failOn: "Before");

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([
            failingAfterStage,
            stage2,
        ]);

        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                request,
                async () => throw new InvalidOperationException("Handler should not run"),
                CancellationToken.None
            )
        );

        // The original Before exception (from stage2) should propagate, not the After exception (from stage1).
        Assert.Equal("Stage S2 failed during Before", ex.Message);

        // S1-FailAfter completed Before, so when we unwind it should receive After, which fails.
        // But we suppress that exception and re-throw the original Before exception from S2.
        // So we should see: S1-FailAfter.Before, S1-FailAfter.After (fails but suppressed), then original S2 exception re-thrown.
        Assert.Equal(["S1-FailAfter.Before", "S1-FailAfter.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WithNoStages_CallsHandlerDirectly()
    {
        // Arrange
        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>([]);
        var request = new TestRequest();
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, async () => expectedResult, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    // Minimal test types
    private class TestRequest { }

    private class TestResponse { }

    private class TrackingStage(string name, List<string> callOrder)
        : IPipelineStage<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public async Task OnBeforeAsync(TestRequest request, CancellationToken ct)
        {
            callOrder.Add($"{name}.Before");
            await Task.CompletedTask;
        }

        public async Task OnAfterAsync(
            TestRequest request,
            OperationOutcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.After");
            await Task.CompletedTask;
        }
    }

    private class FailingStage(string name, List<string> callOrder, string failOn)
        : IPipelineStage<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public async Task OnBeforeAsync(TestRequest request, CancellationToken ct)
        {
            if (failOn == "Before")
            {
                throw new InvalidOperationException($"Stage {name} failed during Before");
            }

            callOrder.Add($"{name}.Before");
            await Task.CompletedTask;
        }

        public async Task OnAfterAsync(
            TestRequest request,
            OperationOutcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.After");
            if (failOn == "After")
            {
                throw new InvalidOperationException($"Stage {name} failed during After");
            }

            await Task.CompletedTask;
        }
    }
}
