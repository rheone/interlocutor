namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests for <see cref="PipelineBehaviorBase{TRequest, TResponse}"/> covering the template method's
/// pass-through default, the enter/exit pairing on both the continue and short-circuit paths, the exit
/// hook's ability to replace the response, and the no-after-hook-on-exception contract.
/// </summary>
public class PipelineBehaviorBaseTests
{
    [Fact]
    public async Task HandleAsync_WithBaseClassBehavior_DefaultHooksAreNoOpAndPassThrough()
    {
        // Arrange: a subclass overriding neither hook should behave exactly like calling next() directly.
        var behavior = new NoOpBehavior();
        var expected = new Outcome<TestResponse, Failure>(new TestResponse());
        var nextRan = false;

        // Act
        var result = await behavior.HandleAsync(
            new TestRequest(),
            async () =>
            {
                nextRan = true;
                return expected;
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(nextRan);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_WhenOnEnterContinues_NextRunsAndOnExitSeesItsResult()
    {
        // Arrange
        var callOrder = new List<string>();
        var handlerResult = new Outcome<TestResponse, Failure>(new TestResponse());
        var behavior = new TrackingBehavior(callOrder, onEnterShortCircuit: null, onExitResponse: null);

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
        Assert.Equal(handlerResult, result);
        Assert.Equal(["Enter", "Next", "Exit"], callOrder);
        Assert.Equal(handlerResult, behavior.ObservedExitResult);
    }

    [Fact]
    public async Task HandleAsync_WhenOnEnterShortCircuits_NextIsSkippedButOnExitStillRunsWithTheShortCircuitResponse()
    {
        // Arrange
        var callOrder = new List<string>();
        var shortCircuitResponse = new Outcome<TestResponse, Failure>(new Failure("declined"));
        var behavior = new TrackingBehavior(callOrder, onEnterShortCircuit: shortCircuitResponse, onExitResponse: null);
        var nextRan = false;

        // Act
        var result = await behavior.HandleAsync(
            new TestRequest(),
            async () =>
            {
                nextRan = true;
                return new Outcome<TestResponse, Failure>(new TestResponse());
            },
            CancellationToken.None
        );

        // Assert: next() never ran, but Exit still saw the short-circuit response, paired with Enter.
        Assert.False(nextRan);
        Assert.Equal(shortCircuitResponse, result);
        Assert.Equal(["Enter", "Exit"], callOrder);
        Assert.Equal(shortCircuitResponse, behavior.ObservedExitResult);
    }

    [Fact]
    public async Task HandleAsync_WhenOnExitOverridesTheResponse_HandleAsyncReturnsTheReplacement()
    {
        // Arrange
        var handlerResult = new Outcome<TestResponse, Failure>(new TestResponse());
        var replacement = new Outcome<TestResponse, Failure>(new Failure("overridden"));
        var behavior = new TrackingBehavior([], onEnterShortCircuit: null, onExitResponse: replacement);

        // Act
        var result = await behavior.HandleAsync(new TestRequest(), async () => handlerResult, CancellationToken.None);

        // Assert: OnExit received next()'s result but the caller-visible response is the replacement.
        Assert.Equal(handlerResult, behavior.ObservedExitResult);
        Assert.Equal(replacement, result);
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrows_ExceptionPropagatesAndOnExitDoesNotRun()
    {
        // Arrange
        var callOrder = new List<string>();
        var behavior = new TrackingBehavior(callOrder, onEnterShortCircuit: null, onExitResponse: null);
        var thrown = new InvalidOperationException("Handler failed");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(new TestRequest(), () => throw thrown, CancellationToken.None)
        );

        Assert.Same(thrown, ex);
        Assert.Equal(["Enter"], callOrder);
        Assert.False(behavior.OnExitRan);
    }

    // Minimal test types - deliberately empty type-identity markers for this test's generic shape, not data holders.
#pragma warning disable S2094
    public class TestRequest { }

    public class TestResponse { }
#pragma warning restore S2094

    private sealed class NoOpBehavior : PipelineBehaviorBase<TestRequest, Outcome<TestResponse, Failure>>;

    /// <summary>
    /// Records enter/next/exit ordering and lets each test configure whether <c>OnEnterAsync</c>
    /// short-circuits and/or <c>OnExitAsync</c> replaces the response, without needing a dedicated subclass
    /// per scenario.
    /// </summary>
    private sealed class TrackingBehavior(
        List<string> callOrder,
        Outcome<TestResponse, Failure>? onEnterShortCircuit,
        Outcome<TestResponse, Failure>? onExitResponse
    ) : PipelineBehaviorBase<TestRequest, Outcome<TestResponse, Failure>>
    {
        public Outcome<TestResponse, Failure>? ObservedExitResult { get; private set; }

        public bool OnExitRan { get; private set; }

        protected override async Task<StageResult<Outcome<TestResponse, Failure>>> OnEnterAsync(
            TestRequest request,
            CancellationToken ct
        )
        {
            callOrder.Add("Enter");
            await Task.CompletedTask.ConfigureAwait(false);
            return onEnterShortCircuit.HasValue
                ? StageResult<Outcome<TestResponse, Failure>>.ShortCircuit(onEnterShortCircuit.Value)
                : StageResult<Outcome<TestResponse, Failure>>.Continue;
        }

        protected override async Task<Outcome<TestResponse, Failure>> OnExitAsync(
            TestRequest request,
            Outcome<TestResponse, Failure> result,
            CancellationToken ct
        )
        {
            callOrder.Add("Exit");
            this.OnExitRan = true;
            this.ObservedExitResult = result;
            await Task.CompletedTask.ConfigureAwait(false);
            return onExitResponse ?? result;
        }
    }
}
