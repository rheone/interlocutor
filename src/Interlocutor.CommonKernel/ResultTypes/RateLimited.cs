namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that the caller was throttled by a rate limiter.
/// </summary>
/// <param name="LimitKey">Identifies which limiter/bucket was hit (e.g., a policy name or partition key).</param>
/// <param name="RetryAfter">An optional hint for how long the caller should wait before retrying.</param>
/// <param name="Message">An optional human-readable description of the rate limit that was exceeded.</param>
/// <remarks>
/// <para>
/// Use <see cref="RateLimited"/> when a handler or pipeline behavior rejects a request because a rate limit was
/// exceeded. <paramref name="LimitKey"/> identifies which limiter/bucket was hit, so callers can distinguish
/// between multiple rate limiters in effect at once (e.g., per-user vs. per-tenant).
/// </para>
/// <para>
/// <paramref name="RetryAfter"/> is generic retry guidance, deliberately not named or shaped after any
/// HTTP-specific concept. This is a library-level type usable outside HTTP contexts; HTTP-specific framing (such as
/// mapping to a <c>Retry-After</c> response header) belongs in the ASP.NET Core integration layer, not here.
/// </para>
/// </remarks>
public readonly record struct RateLimited(string LimitKey, TimeSpan? RetryAfter = null, string? Message = null)
    : IRateLimitedFailureCase
{
    /// <summary>
    /// Returns a human-readable representation of this rate-limit rejection.
    /// </summary>
    /// <returns>
    /// <c>"RateLimited: {LimitKey}"</c> when <see cref="Message"/> is <see langword="null"/>; otherwise
    /// <c>"RateLimited: {LimitKey}, {Message}"</c>.
    /// </returns>
    public override string ToString() =>
        this.Message is null ? $"RateLimited: {this.LimitKey}" : $"RateLimited: {this.LimitKey}, {this.Message}";
}
