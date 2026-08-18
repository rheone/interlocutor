namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// An <see cref="IRateLimiter"/> implementation that wraps a caller-supplied delegate instead of requiring a
/// dedicated class - useful for inline configuration in tests or small applications.
/// </summary>
/// <param name="check">The delegate invoked from <see cref="CheckAsync"/>.</param>
public sealed class DelegateRateLimiter(Func<string, object, CancellationToken, Task<RateLimitResult>> check) : IRateLimiter
{
    /// <inheritdoc/>
    public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct) =>
        check(limitKey, request, ct);
}
