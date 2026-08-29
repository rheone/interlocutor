namespace TodoTasks.Api.Tests.Unit.Infrastructure;

using Microsoft.Extensions.Logging;
using NSubstitute;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Unit tests for <see cref="SlowHandlerWarningBehavior{TRequest, TResponse}"/> - proves it measures only
/// the handler's own time, isolated from whatever ran ahead of it in the chain, exactly as
/// <c>documentation/beginners-guide-todo-api.md</c> Step 11 explains a custom behavior registered
/// innermost should.
/// </summary>
public sealed class SlowHandlerWarningBehaviorTests
{
    private readonly ILogger<SlowHandlerWarningBehavior<SlowRequest, string>> logger = Substitute.For<
        ILogger<SlowHandlerWarningBehavior<SlowRequest, string>>
    >();

    [Fact]
    public async Task Handle_HandlerFasterThanThreshold_LogsNoWarning_Test()
    {
        // Arrange
        var behavior = new SlowHandlerWarningBehavior<SlowRequest, string>(this.logger);
        static Task<string> FastHandler() => Task.FromResult("done");

        // Act
        var result = await behavior.Handle(new SlowRequest(), FastHandler, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("done", result);
        this.logger.DidNotReceive()
            .Log(
                Arg.Is(LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task Handle_HandlerSlowerThanThreshold_LogsAWarning_Test()
    {
        // Arrange - far slower than the 250ms threshold, so the delay wins decisively (no flake).
        var behavior = new SlowHandlerWarningBehavior<SlowRequest, string>(this.logger);
        static async Task<string> SlowHandler()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400), TestContext.Current.CancellationToken);
            return "done";
        }

        // Act
        var result = await behavior.Handle(new SlowRequest(), SlowHandler, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("done", result);
        this.logger.Received(1)
            .Log(
                Arg.Is(LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task Handle_RegisteredInnermost_MeasuresOnlyTheHandler_NotWhateverRanAheadOfIt_Test()
    {
        // Arrange - simulates registration position mechanically: everything "ahead of" this behavior in
        // the chain (logging, auditing, authorization, validation, the transaction's own begin) already
        // ran and returned *before* this behavior's Handle is ever called, exactly like nested pipeline
        // dispatch. That outer cost alone (300ms) already exceeds the 250ms threshold, but the handler
        // this behavior actually wraps is fast (10ms) - proving this behavior's stopwatch starts at its
        // own ingress, not at the dispatch's start.
        var behavior = new SlowHandlerWarningBehavior<SlowRequest, string>(this.logger);
        await Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken); // Ahead of this behavior - never measured by it.
        static async Task<string> FastHandler()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken);
            return "done";
        }

        // Act
        await behavior.Handle(new SlowRequest(), FastHandler, TestContext.Current.CancellationToken);

        // Assert - no warning, even though the total elapsed time since the test started is well past the
        // threshold, because none of that 300ms was inside this behavior's own next() call.
        this.logger.DidNotReceive()
            .Log(
                Arg.Is(LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    /// <summary>A minimal request type - this behavior is unconstrained, so no Interlocutor marker is needed to exercise it.</summary>
#pragma warning disable S2094 // Deliberately empty: a type-identity marker, not a data holder.
    public sealed record SlowRequest;
#pragma warning restore S2094
}
