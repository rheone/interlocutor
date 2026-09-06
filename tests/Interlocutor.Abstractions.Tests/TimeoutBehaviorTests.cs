namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

public class TimeoutBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNextExceedsBudget_ShortCircuitsWithTimeoutCarryingBudgetAndElapsed()
    {
        // Arrange - a handler far slower than the budget, so the delay wins the race decisively (no flake).
        var budget = TimeSpan.FromMilliseconds(50);
        var behavior = new TimeoutBehavior<TimeBoundQuery, Outcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);
        static async Task<Outcome<string, Timeout>> NextAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
            return new Outcome<string, Timeout>("too slow");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

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
        var behavior = new TimeoutBehavior<TimeBoundQuery, Outcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);

        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<Outcome<string, Timeout>>();
        Task<Outcome<string, Timeout>> NextAsync()
        {
            // VSTHRD003/IDISP013 both read this as a "foreign" task escaping the using scope below - it's
            // deliberately never-completing (a stand-in for a slow handler losing the race to cts's own
            // cancellation), not a task whose start this method owns or needs to await within the using block.
#pragma warning disable VSTHRD003
#pragma warning disable IDISP013
            return tcs.Task; // never completes on its own
#pragma warning restore IDISP013
#pragma warning restore VSTHRD003
        }

        cts.CancelAfter(TimeSpan.FromMilliseconds(20));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.HandleAsync(request, NextAsync, cts.Token));
    }

    [Fact]
    public async Task Handle_WhenNextCompletesWithinBudget_PassesThroughResultUnchanged()
    {
        // Arrange - a generous budget and a fast handler, so next() wins the race decisively.
        var budget = TimeSpan.FromMilliseconds(500);
        var behavior = new TimeoutBehavior<TimeBoundQuery, Outcome<string, Timeout>>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(budget);
        static Task<Outcome<string, Timeout>> NextAsync()
        {
            return Task.FromResult<Outcome<string, Timeout>>(new Outcome<string, Timeout>("fast enough"));
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

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
        var behavior = new TimeoutBehavior<TimeBoundQuery, Outcome<string, Timeout>>(
            policy,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundQuery(TimeSpan.FromSeconds(30)); // request declares a much larger budget
        static async Task<Outcome<string, Timeout>> NextAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
            return new Outcome<string, Timeout>("too slow");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        var timeout = Assert.IsType<Timeout>(result.Value);
        Assert.Equal(overriddenBudget, timeout.Budget);
    }

    [Fact]
    public async Task Handle_WithCaseLessOutcomeResponse_ThrowsNamedExceptionWhenBudgetElapses()
    {
        // The case-less, non-generic Outcome (see its own remarks) is the specific incompatibility this
        // ticket calls out: pairing ITimeoutRequest with a response that has no Timeout case to construct into
        // at all is a configuration mistake, surfaced loudly the moment the budget actually elapses - not a
        // silent no-op.
        // Arrange
        var budget = TimeSpan.FromMilliseconds(50);
        var behavior = new TimeoutBehavior<CaseLessTimeBoundQuery, Outcome>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CaseLessTimeBoundQuery(budget);
        static async Task<Outcome> NextAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
            return default(Outcome);
        }

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(request, NextAsync, CancellationToken.None)
        );

        // Assert
        Assert.Contains("Timeout", ex.Message);
    }

    public record TimeBoundQuery(TimeSpan Budget) : IQuery<Outcome<string, Timeout>>, ITimeoutRequest;

    /// <summary>A request implementing <see cref="ITimeoutRequest"/> whose response is the case-less <see cref="Outcome"/>.</summary>
    public record CaseLessTimeBoundQuery(TimeSpan Budget) : IQuery<Outcome>, ITimeoutRequest;
}
