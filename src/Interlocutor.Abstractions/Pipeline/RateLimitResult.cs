namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// The outcome of an <see cref="IRateLimiter"/> check.
/// </summary>
/// <param name="IsAllowed"><see langword="true"/> if the request is allowed to proceed; otherwise <see langword="false"/>.</param>
/// <param name="RetryAfter">
/// An optional hint for how long the caller should wait before retrying, propagated into the
/// <see cref="CommonKernel.ResultTypes.RateLimited"/> case's own <c>RetryAfter</c> when <paramref name="IsAllowed"/> is
/// <see langword="false"/>.
/// </param>
public readonly record struct RateLimitResult(bool IsAllowed, TimeSpan? RetryAfter = null);
