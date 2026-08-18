namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

public class TimeoutBehaviorTests
{
    public record TimeBoundQuery(TimeSpan Budget) : IQuery<OperationOutcome<string, Timeout>>, IHasTimeout;

    [Fact]
    public async Task Handle_WhenNextExceedsBudget_ShortCircuitsWithTimeoutCarryingBudgetAndElapsed()
    {
        // Arrange - a handler far slower than the budget, so the delay wins the race decisively (no flake).
        var budget = TimeSpan.FromMilliseconds(50);
        var behavior = new TimeoutBehavior<TimeBoundQuery, OperationOutcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);
        Func<Task<OperationOutcome<string, Timeout>>> next = async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
            return new OperationOutcome<string, Timeout>("too slow");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var timeout = Assert.IsType<Timeout>(result.Value);
        Assert.Equal(budget, timeout.Budget);
        Assert.True(timeout.Elapsed >= budget, $"Expected Elapsed ({timeout.Elapsed}) >= Budget ({budget}).");
        Assert.True(
            timeout.Elapsed < TimeSpan.FromMilliseconds(500),
            $"Expected Elapsed ({timeout.Elapsed}) to reflect the actual race duration, not the handler's full delay."
        );
    }

    [Fact]
    public async Task Handle_WhenAmbientCancellationWinsTheRace_PropagatesOperationCanceledExceptionNotTimeout()
    {
        // Arrange - a generous budget the handler never gets a chance to exceed, and an ambient token
        // cancelled well before the budget could elapse. The delay task should "win" purely because of
        // cancellation, not because the budget genuinely ran out.
        var budget = TimeSpan.FromSeconds(30);
        var behavior = new TimeoutBehavior<TimeBoundQuery, OperationOutcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);

        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<OperationOutcome<string, Timeout>>();
        Func<Task<OperationOutcome<string, Timeout>>> next = () => tcs.Task; // never completes on its own

        cts.CancelAfter(TimeSpan.FromMilliseconds(20));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(request, next, cts.Token));
    }

    [Fact]
    public async Task Handle_WhenNextCompletesWithinBudget_PassesThroughResultUnchanged()
    {
        // Arrange - a generous budget and a fast handler, so next() wins the race decisively.
        var budget = TimeSpan.FromMilliseconds(500);
        var behavior = new TimeoutBehavior<TimeBoundQuery, OperationOutcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);
        Func<Task<OperationOutcome<string, Timeout>>> next = () =>
            Task.FromResult<OperationOutcome<string, Timeout>>(new OperationOutcome<string, Timeout>("fast enough"));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("fast enough", result.Value);
    }

    [Fact]
    public async Task Handle_ResolvesBudgetViaTimeoutPolicy()
    {
        // Arrange - DelegateTimeoutPolicy overrides the request's own budget, and the behavior should race
        // against the resolved value, not the request's raw Budget.
        var overriddenBudget = TimeSpan.FromMilliseconds(50);
        var policy = new DelegateTimeoutPolicy(_ => overriddenBudget);
        var behavior = new TimeoutBehavior<TimeBoundQuery, OperationOutcome<string, Timeout>>(
            policy,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(TimeSpan.FromSeconds(30)); // request declares a much larger budget
        Func<Task<OperationOutcome<string, Timeout>>> next = async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
            return new OperationOutcome<string, Timeout>("too slow");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var timeout = Assert.IsType<Timeout>(result.Value);
        Assert.Equal(overriddenBudget, timeout.Budget);
    }
}
