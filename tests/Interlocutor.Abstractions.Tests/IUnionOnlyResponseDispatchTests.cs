namespace Interlocutor.Abstractions.Tests;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Proves that every pipeline behavior in this package that constrains <c>TResponse</c>
/// (<see cref="AuthorizationBehavior{TRequest, TResponse}"/>, <see cref="CancellationBehavior{TRequest, TResponse}"/>,
/// <see cref="TimeoutBehavior{TRequest, TResponse}"/>, <see cref="RateLimitedBehavior{TRequest, TResponse}"/>)
/// accepts and dispatches correctly through <see cref="UnionOnlyResponse"/> - a hand-rolled response type that
/// implements <see cref="IUnion"/> directly and never implements <see cref="IOutcome"/> at all. Before the
/// <c>IOutcome</c>-to-<c>IUnion</c> constraint swap, every one of these behaviors read
/// <c>where TResponse : IOutcome</c>, so a response type like this one would not even have compiled as their
/// type argument, regardless of its runtime shape.
/// </summary>
public class IUnionOnlyResponseDispatchTests
{
    [Fact]
    public async Task AuthorizationBehavior_HandlesUnionOnlyResponse_ShortCircuitsWithForbidden()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<UnionOnlyResponse, Forbidden>(UnionOnlyResponse.FromForbidden)
            .Build();

        var behavior = new AuthorizationBehavior<ProtectedUnionOnlyQuery, UnionOnlyResponse>(policyEvaluator, caseRegistry);
        var request = new ProtectedUnionOnlyQuery();
        static Task<UnionOnlyResponse> NextAsync() => throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.IsType<Forbidden>(result.Value);
    }

    [Fact]
    public async Task AuthorizationBehavior_HandlesUnionOnlyResponse_PassesThroughWhenAllowed()
    {
        // Arrange
        var policyEvaluator = Substitute.For<IPolicyEvaluator>();
        policyEvaluator
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var behavior = new AuthorizationBehavior<ProtectedUnionOnlyQuery, UnionOnlyResponse>(
            policyEvaluator,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ProtectedUnionOnlyQuery();
        static Task<UnionOnlyResponse> NextAsync() => Task.FromResult(UnionOnlyResponse.FromSuccess("ok"));

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task CancellationBehavior_HandlesUnionOnlyResponse_ShortCircuitsWithCancelled()
    {
        // Arrange
        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<UnionOnlyResponse, Cancelled>(UnionOnlyResponse.FromCancelled)
            .Build();

        var behavior = new CancellationBehavior<CancellableUnionOnlyQuery, UnionOnlyResponse>(caseRegistry);
        var request = new CancellableUnionOnlyQuery();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;

        Task<UnionOnlyResponse> NextAsync() => throw new OperationCanceledException(ct);

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, ct);

        // Assert
        Assert.IsType<Cancelled>(result.Value);
    }

    [Fact]
    public async Task CancellationBehavior_HandlesUnionOnlyResponse_PassesThroughWhenNotCancelled()
    {
        // Arrange
        var behavior = new CancellationBehavior<CancellableUnionOnlyQuery, UnionOnlyResponse>(
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CancellableUnionOnlyQuery();
        static Task<UnionOnlyResponse> NextAsync() => Task.FromResult(UnionOnlyResponse.FromSuccess("ok"));

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task TimeoutBehavior_HandlesUnionOnlyResponse_ShortCircuitsWithTimeout()
    {
        // Arrange - a handler far slower than the budget, so the delay wins the race decisively (no flake).
        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<UnionOnlyResponse, Timeout>(UnionOnlyResponse.FromTimeout)
            .Build();

        var behavior = new TimeoutBehavior<TimeBoundUnionOnlyQuery, UnionOnlyResponse>(
            DefaultTimeoutPolicy.Instance,
            caseRegistry
        );
        var request = new TimeBoundUnionOnlyQuery(TimeSpan.FromMilliseconds(50));
        static async Task<UnionOnlyResponse> NextAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
            return UnionOnlyResponse.FromSuccess("too slow");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.IsType<Timeout>(result.Value);
    }

    [Fact]
    public async Task TimeoutBehavior_HandlesUnionOnlyResponse_PassesThroughWhenWithinBudget()
    {
        // Arrange - a generous budget and a fast handler, so next() wins the race decisively.
        var behavior = new TimeoutBehavior<TimeBoundUnionOnlyQuery, UnionOnlyResponse>(
            DefaultTimeoutPolicy.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new TimeBoundUnionOnlyQuery(TimeSpan.FromMilliseconds(500));
        static Task<UnionOnlyResponse> NextAsync() => Task.FromResult(UnionOnlyResponse.FromSuccess("fast enough"));

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("fast enough", result.Value);
    }

    [Fact]
    public async Task RateLimitedBehavior_HandlesUnionOnlyResponse_ShortCircuitsWithRateLimited()
    {
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false)));

        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<UnionOnlyResponse, RateLimited>(UnionOnlyResponse.FromRateLimited)
            .Build();

        var behavior = new RateLimitedBehavior<ThrottledUnionOnlyQuery, UnionOnlyResponse>(rateLimiter, caseRegistry);
        var request = new ThrottledUnionOnlyQuery();
        static Task<UnionOnlyResponse> NextAsync() => throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.IsType<RateLimited>(result.Value);
    }

    [Fact]
    public async Task RateLimitedBehavior_HandlesUnionOnlyResponse_PassesThroughWhenAllowed()
    {
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: true)));

        var behavior = new RateLimitedBehavior<ThrottledUnionOnlyQuery, UnionOnlyResponse>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledUnionOnlyQuery();
        static Task<UnionOnlyResponse> NextAsync() => Task.FromResult(UnionOnlyResponse.FromSuccess("ok"));

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("ok", result.Value);
    }

    public sealed record ProtectedUnionOnlyQuery : IRequiresPolicy
    {
        public string PolicyName => "CanRead";
    }

    public sealed record CancellableUnionOnlyQuery : ICancellable;

    public sealed record TimeBoundUnionOnlyQuery(TimeSpan Budget) : IHasTimeout;

    public sealed record ThrottledUnionOnlyQuery : IRateLimited
    {
        public string LimitKey => "per-user";
    }

    /// <summary>
    /// A hand-rolled response type implementing <see cref="IUnion"/> directly - never
    /// <see cref="IOutcome"/> - proving <c>IOutcome</c> is no longer required to satisfy any <c>TResponse</c>
    /// constraint in this package. Not a compiler-generated <c>union</c> either (no case constructor for
    /// <see cref="Interlocutor.Abstractions.Pipeline.UnionCaseFactory"/> to find reflectively), so
    /// short-circuiting through it still needs a custom <see cref="IOutcomeCaseRegistry"/> factory - exactly
    /// as a hand-rolled <see cref="IOutcome"/> implementation would have needed before this ticket.
    /// </summary>
    public sealed class UnionOnlyResponse : IUnion
    {
        private UnionOnlyResponse(object? value)
        {
            this.Value = value;
        }

        public object? Value { get; }

        public static UnionOnlyResponse FromSuccess(string value) => new(value);

        public static UnionOnlyResponse FromForbidden(Forbidden forbidden) => new(forbidden);

        public static UnionOnlyResponse FromCancelled(Cancelled cancelled) => new(cancelled);

        public static UnionOnlyResponse FromTimeout(Timeout timeout) => new(timeout);

        public static UnionOnlyResponse FromRateLimited(RateLimited rateLimited) => new(rateLimited);
    }
}
