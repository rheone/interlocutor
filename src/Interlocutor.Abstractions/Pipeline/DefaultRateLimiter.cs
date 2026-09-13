namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Default <see cref="IRateLimiter"/> implementation suitable for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation always allows the request, granting all rate-limit checks. Suitable for:
/// <list type="bullet">
///   <item><description>Development environments where throttling is not yet implemented.</description></item>
///   <item><description>Testing scenarios where rate limiting should not block test execution.</description></item>
///   <item><description>Prototypes and proof-of-concepts.</description></item>
/// </list>
/// </para>
/// <para>
/// In production, replace this with a real rate-limiting implementation before deploying.
/// </para>
/// </remarks>
public sealed class DefaultRateLimiter : IRateLimiter
{
    /// <summary>Gets the shared instance, for registering this always-allow limiter as the <see cref="IRateLimiter"/> without allocating a new one per registration.</summary>
    public static readonly DefaultRateLimiter Instance = new();

    /// <inheritdoc/>
    public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct) =>
        Task.FromResult(new RateLimitResult(IsAllowed: true));
}
