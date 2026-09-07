namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

public class RateLimitedBehaviorTests
{
    [Fact]
    public async Task Handle_AllowsRequestWhenWithinLimit()
    {
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: true)));

        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        static Task<Outcome<string, RateLimited>> NextAsync()
        {
            return Task.FromResult<Outcome<string, RateLimited>>(new Outcome<string, RateLimited>("success"));
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("success", result.Value);
        await rateLimiter.Received(1).CheckAsync("per-user", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ReturnsRateLimitedWhenLimitExceeded()
    {
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false)));

        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        static Task<Outcome<string, RateLimited>> NextAsync()
        {
            throw new InvalidOperationException("Handler should not be called");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert - Should return RateLimited instead of calling next
        var rateLimited = Assert.IsType<RateLimited>(result.Value);
        Assert.Equal("per-user", rateLimited.LimitKey);
        await rateLimiter.Received(1).CheckAsync("per-user", Arg.Is(request), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_PropagatesRetryAfterFromLimiterIntoRateLimitedCase()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false, RetryAfter: retryAfter)));

        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        static Task<Outcome<string, RateLimited>> NextAsync()
        {
            throw new InvalidOperationException("Should not be called");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        var rateLimited = Assert.IsType<RateLimited>(result.Value);
        Assert.Equal(retryAfter, rateLimited.RetryAfter);
    }

    [Fact]
    public async Task Handle_DoesNotCallNextWhenLimitExceeded()
    {
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false)));

        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        var nextCalled = false;

        Task<Outcome<string, RateLimited>> NextAsync()
        {
            nextCalled = true;
            return Task.FromResult<Outcome<string, RateLimited>>(new Outcome<string, RateLimited>("success"));
        }

        // Act
        await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert - next should not be called
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WithResponseTypeThatHasNoRateLimitedCase_ThrowsNamedExceptionOnThrottle()
    {
        // TResponse is an arbitrary IOutcome the caller chose, constructed reflectively from a
        // RateLimited case constructor when the limit is exceeded. A response shape with no such case
        // (like Outcome<string, NotFound> here) can't express "throttled" at all, so throttling
        // must fail loudly and specifically rather than silently succeeding or crashing obscurely.
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false)));

        var behavior = new RateLimitedBehavior<MisshapenThrottledQuery, Outcome<string, NotFound>>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new MisshapenThrottledQuery();
        static Task<Outcome<string, NotFound>> NextAsync()
        {
            throw new InvalidOperationException("Should not be called");
        }

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(request, NextAsync, CancellationToken.None)
        );

        // Assert
        Assert.Contains("RateLimited", ex.Message);
    }

    [Fact]
    public async Task Handle_WithCaseLessOutcomeResponse_ThrowsNamedExceptionOnThrottle()
    {
        // The case-less, non-generic Outcome (see its own remarks) is the specific incompatibility this
        // ticket calls out: pairing IRateLimitedRequest with a response that has no RateLimited case to construct
        // into at all is a configuration mistake, surfaced loudly the moment the limiter actually throttles -
        // not a silent no-op.
        // Arrange
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter
            .CheckAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RateLimitResult(IsAllowed: false)));

        var behavior = new RateLimitedBehavior<CaseLessThrottledQuery, Outcome>(
            rateLimiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CaseLessThrottledQuery();
        static Task<Outcome> NextAsync()
        {
            throw new InvalidOperationException("Should not be called");
        }

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(request, NextAsync, CancellationToken.None)
        );

        // Assert
        Assert.Contains("RateLimited", ex.Message);
    }

    [Fact]
    public async Task Handle_UsingDefaultRateLimiter_AlwaysAllows()
    {
        // Arrange
        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            DefaultRateLimiter.Instance,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        static Task<Outcome<string, RateLimited>> NextAsync()
        {
            return Task.FromResult<Outcome<string, RateLimited>>(new Outcome<string, RateLimited>("success"));
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task Handle_UsingDelegateRateLimiter_InvokesSuppliedDelegate()
    {
        // Arrange
        var delegateCalled = false;
        var limiter = new DelegateRateLimiter(
            (_, _, _) =>
            {
                delegateCalled = true;
                return Task.FromResult(new RateLimitResult(IsAllowed: false, RetryAfter: TimeSpan.FromSeconds(5)));
            }
        );

        var behavior = new RateLimitedBehavior<ThrottledQuery, Outcome<string, RateLimited>>(
            limiter,
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new ThrottledQuery("test");
        static Task<Outcome<string, RateLimited>> NextAsync()
        {
            throw new InvalidOperationException("Should not be called");
        }

        // Act
        var result = await behavior.HandleAsync(request, NextAsync, CancellationToken.None);

        // Assert
        Assert.True(delegateCalled);
        var rateLimited = Assert.IsType<RateLimited>(result.Value);
        Assert.Equal(TimeSpan.FromSeconds(5), rateLimited.RetryAfter);
    }

    public record ThrottledQuery(string Name) : IQuery<Outcome<string, RateLimited>>, IRateLimitedRequest
    {
        public string LimitKey => "per-user";
    }

    /// <summary>A request declaring a limit key whose response has no <see cref="RateLimited"/> case at all.</summary>
    public record MisshapenThrottledQuery : IQuery<Outcome<string, NotFound>>, IRateLimitedRequest
    {
        public string LimitKey => "per-user";
    }

    /// <summary>A request implementing <see cref="IRateLimitedRequest"/> whose response is the case-less <see cref="Outcome"/>.</summary>
    public record CaseLessThrottledQuery : IQuery<Outcome>, IRateLimitedRequest
    {
        public string LimitKey => "per-user";
    }
}
