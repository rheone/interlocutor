namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Minimal rate-limiting abstraction that avoids coupling to any specific throttling library.</summary>
/// <remarks>
/// <para>
/// Implement <see cref="IRateLimiter"/> to provide throttling decisions for requests implementing
/// <see cref="Markers.IRateLimitedRequest"/>. Register your implementation in DI, and
/// <see cref="RateLimitedBehavior{TRequest, TResponse}"/> will invoke it to check limits.
/// </para>
/// <para>
/// This abstraction is deliberately minimal to avoid framework dependencies. Your implementation can delegate
/// to <c>System.Threading.RateLimiting</c>, a distributed limiter backed by Redis, or any throttling system.
/// </para>
/// </remarks>
public interface IRateLimiter
{
    /// <summary>Checks whether the request is allowed to proceed under the named limit.</summary>
    /// <param name="limitKey">The limit/bucket key to check, from <see cref="Markers.IRateLimitedRequest.LimitKey"/>.</param>
    /// <param name="request">The request context for the rate-limit check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="RateLimitResult"/> describing the decision.</returns>
    Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct);
}
