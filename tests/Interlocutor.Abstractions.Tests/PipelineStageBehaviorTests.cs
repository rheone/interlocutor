namespace Interlocutor.Abstractions.Tests.Pipeline;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for <see cref="PipelineStageBehavior{TRequest,TResponse}"/> covering before/after ordering,
/// short-circuit unwind, handler-throw behavior, and after-hook exception handling.
/// </summary>
public class PipelineStageBehaviorTests
{
    private static ILogger<PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>> NewLogger() =>
        Substitute.For<ILogger<PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>>>();

    [Fact]
    public async Task Handle_WithAllStagesCompleted_InvokesAfterHooksInReverseOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new TrackingStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            NewLogger()
        );

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
    public async Task Handle_WhenSecondStageBeforeThrows_FirstStageBehindReceivesAfter()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new FailingBeforeStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            NewLogger()
        );

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

        // S1 completed Before, so S1 After must run. S2's Before threw (never completes), S3 never ran
        // Before. S2 and S3 After should not run because their Before never completed.
        Assert.Equal(["S1.Before", "S1.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_AfterHooksDoNotRun()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new TrackingStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            NewLogger()
        );

        var request = new TestRequest();
        var handlerException = new InvalidOperationException("Handler failed");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, async () => throw handlerException, CancellationToken.None)
        );

        Assert.Same(handlerException, ex);

        // All stages completed Before, but the handler threw, so no After hooks run at all.
        Assert.Equal(["S1.Before", "S2.Before", "S3.Before"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenBeforeThrowsAndAfterFails_OriginalBeforeExceptionPropagatesAndAfterExceptionIsLogged()
    {
        // Arrange
        var callOrder = new List<string>();
        var failingAfterStage = new FailingAfterStage("S1-FailAfter", callOrder);
        var stage2 = new FailingBeforeStage("S2", callOrder);
        var logger = NewLogger();

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [failingAfterStage, stage2],
            logger
        );

        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                request,
                async () => throw new InvalidOperationException("Handler should not run"),
                CancellationToken.None
            )
        );

        // The original Before exception (from stage2) propagates, not the After exception (from stage1) --
        // the After exception is logged and swallowed by default instead of masking the real failure.
        Assert.Equal("Stage S2 failed during Before", ex.Message);
        Assert.Equal(["S1-FailAfter.Before", "S1-FailAfter.After"], callOrder);

        logger
            .Received(1)
            .Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task Handle_WhenAfterHookThrowsAndRethrowNotEnabled_ExceptionIsLoggedAndUnwindContinues()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new FailingAfterStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);
        var logger = NewLogger();

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            logger
        );

        var request = new TestRequest();
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, async () => expectedResult, CancellationToken.None);

        // Assert: every stage's After still ran, despite S2's After throwing.
        Assert.Equal(expectedResult, result);
        Assert.Equal(
            ["S1.Before", "S2.Before", "S3.Before", "S3.After", "S2.After", "S1.After"],
            callOrder
        );

        logger
            .Received(1)
            .Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task Handle_WhenAfterHookThrowsAndRethrowEnabled_ExceptionPropagatesAndUnwindStops()
    {
        // Arrange
        var callOrder = new List<string>();
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new FailingAfterStage("S2", callOrder);
        var stage3 = new TrackingStage("S3", callOrder);
        var logger = NewLogger();

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            logger,
            rethrowAfterHookExceptions: true
        );

        var request = new TestRequest();
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, async () => expectedResult, CancellationToken.None)
        );

        Assert.Equal("Stage S2 failed during After", ex.Message);

        // S3's After ran, S2's After ran (and threw, stopping the unwind), S1's After never ran.
        Assert.Equal(["S1.Before", "S2.Before", "S3.Before", "S3.After", "S2.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WithNoStages_CallsHandlerDirectly()
    {
        // Arrange
        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [],
            NewLogger()
        );
        var request = new TestRequest();
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, async () => expectedResult, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task Handle_WhenBeforeHookShortCircuits_HandlerDoesNotRunAndAfterUnwindsForCompletedStages()
    {
        // Arrange
        var callOrder = new List<string>();
        var shortCircuitResponse = new OperationOutcome<TestResponse, Failure>(new Failure("declined"));
        var stage1 = new TrackingStage("S1", callOrder);
        var stage2 = new ShortCircuitingStage("S2", callOrder, shortCircuitResponse);
        var stage3 = new TrackingStage("S3", callOrder);
        var handlerRan = false;

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage1, stage2, stage3],
            NewLogger()
        );

        var request = new TestRequest();

        // Act
        var result = await behavior.Handle(
            request,
            async () =>
            {
                handlerRan = true;
                return new OperationOutcome<TestResponse, Failure>(new TestResponse());
            },
            CancellationToken.None
        );

        // Assert: the handler and S3's Before never ran; S1 and S2's After both ran, in reverse order,
        // each receiving the short-circuit response. The behavior returns it as an ordinary result.
        Assert.False(handlerRan);
        Assert.Equal(shortCircuitResponse, result);
        Assert.Equal(["S1.Before", "S2.Before", "S2.After", "S1.After"], callOrder);
    }

    [Fact]
    public async Task Handle_WhenBeforeHookShortCircuitsWithDefaultValuedResponse_IsDistinguishableFromContinue()
    {
        // Arrange: OperationOutcome<TestResponse, Failure> is a struct, so its default value is
        // distinguishable from a real short-circuit only via StageResult.ShortCircuited, not by value.
        var handlerRan = false;
        var stage = new ShortCircuitingStage("S1", [], default);

        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [stage],
            NewLogger()
        );

        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            async () =>
            {
                handlerRan = true;
                return new OperationOutcome<TestResponse, Failure>(new TestResponse());
            },
            CancellationToken.None
        );

        // Assert: the handler was skipped even though the short-circuit response is bit-for-bit the
        // default value -- StageResult's flag, not the response's value, drove the decision.
        Assert.False(handlerRan);
        Assert.Equal(default, result);
    }

    [Fact]
    public async Task Handle_WithBaseClassStage_DefaultHooksAreNoOpAndContinue()
    {
        // Arrange: a stage that overrides neither hook should behave exactly like no stage at all.
        var behavior = new PipelineStageBehavior<TestRequest, OperationOutcome<TestResponse, Failure>>(
            [new NoOpStage()],
            NewLogger()
        );
        var expectedResult = new OperationOutcome<TestResponse, Failure>(new TestResponse());

        // Act
        var result = await behavior.Handle(new TestRequest(), async () => expectedResult, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    // Minimal test types
    public class TestRequest { }

    public class TestResponse { }

    private sealed class NoOpStage : PipelineStageBase<TestRequest, OperationOutcome<TestResponse, Failure>>;

    private sealed class TrackingStage(string name, List<string> callOrder)
        : PipelineStageBase<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public override async Task<StageResult<OperationOutcome<TestResponse, Failure>>> OnBeforeAsync(
            TestRequest request,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.Before");
            await Task.CompletedTask;
            return StageResult<OperationOutcome<TestResponse, Failure>>.Continue;
        }

        public override async Task OnAfterAsync(
            TestRequest request,
            OperationOutcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.After");
            await Task.CompletedTask;
        }
    }

    private sealed class ShortCircuitingStage(
        string name,
        List<string> callOrder,
        OperationOutcome<TestResponse, Failure> response
    ) : PipelineStageBase<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public override async Task<StageResult<OperationOutcome<TestResponse, Failure>>> OnBeforeAsync(
            TestRequest request,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.Before");
            await Task.CompletedTask;
            return StageResult<OperationOutcome<TestResponse, Failure>>.ShortCircuit(response);
        }

        public override async Task OnAfterAsync(
            TestRequest request,
            OperationOutcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.After");
            await Task.CompletedTask;
        }
    }

    private sealed class FailingBeforeStage(string name, List<string> callOrder)
        : PipelineStageBase<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public override Task<StageResult<OperationOutcome<TestResponse, Failure>>> OnBeforeAsync(
            TestRequest request,
            CancellationToken ct
        ) => throw new InvalidOperationException($"Stage {name} failed during Before");

        public override async Task OnAfterAsync(
            TestRequest request,
            OperationOutcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.After");
            await Task.CompletedTask;
        }
    }

    private sealed class FailingAfterStage(string name, List<string> callOrder)
        : PipelineStageBase<TestRequest, OperationOutcome<TestResponse, Failure>>
    {
        public override async Task<StageResult<OperationOutcome<TestResponse, Failure>>> OnBeforeAsync(
            TestRequest request,
            CancellationToken ct
        )
        {
            callOrder.Add($"{name}.Before");
            await Task.CompletedTask;
            return StageResult<OperationOutcome<TestResponse, Failure>>.Continue;
        }

        public override Task OnAfterAsync(TestRequest request, OperationOutcome<TestResponse, Failure> result, CancellationToken ct)
        {
            callOrder.Add($"{name}.After");
            throw new InvalidOperationException($"Stage {name} failed during After");
        }
    }
}
