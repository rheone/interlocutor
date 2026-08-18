namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request that is subject to rate limiting before execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries when access should be throttled by a rate limiter.
/// The <see cref="Pipeline.RateLimitedBehavior{TRequest, TResponse}"/> pipeline behavior intercepts these
/// requests, checks the limit via <see cref="Pipeline.IRateLimiter"/>, and short-circuits with
/// <c>RateLimited</c> if the check disallows the request. This keeps throttling logic out of handlers and
/// enables consistent rate-limit enforcement across your application.
/// </para>
/// <para>
/// The <see cref="LimitKey"/> identifies which limiter/bucket applies to this request (e.g., a policy name
/// or partition key such as a user or tenant identifier). Your <see cref="Pipeline.IRateLimiter"/>
/// implementation interprets it.
/// </para>
/// </remarks>
public interface IRateLimited
{
    /// <summary>
    /// Gets the key identifying which rate limiter/bucket applies to this request.
    /// </summary>
    /// <value>
    /// A partition key recognized by the rate-limiting infrastructure, e.g. a policy name, user id, or
    /// tenant id.
    /// </value>
    string LimitKey { get; }
}
