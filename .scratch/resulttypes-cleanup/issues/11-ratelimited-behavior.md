# 11 - RateLimitedBehavior + IRateLimited + IRateLimiter

## Labels

ready-for-agent

**What to build:** A new opt-in pipeline behavior producing the `RateLimited` outcome case, mirroring `AuthorizationBehavior`'s marker+evaluator shape. `IRateLimited` is a marker interface a request implements to declare `string LimitKey`. `IRateLimiter` (`Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct)`, with `RateLimitResult(bool IsAllowed, TimeSpan? RetryAfter = null)`, plus `DefaultRateLimiter` always-allows and `DelegateRateLimiter` for inline configuration, mirroring `IPolicyEvaluator`'s defaults) supplies the actual throttling decision. `RateLimitedBehavior<TRequest, TResponse>` calls the limiter before `next()`; on disallowed, short-circuits with `RateLimited`.

**Blocked by:** 03 (IOutcomeCaseRegistry extension-point), 05 (new case types)

- [x] `IRateLimited` marker interface (`string LimitKey { get; }`) exists in `Interlocutor.Abstractions.Markers`
- [x] `IRateLimiter`, `RateLimitResult`, `DefaultRateLimiter`, `DelegateRateLimiter` exist in `Interlocutor.Abstractions.Pipeline`
- [x] `RateLimitedBehavior<TRequest, TResponse>` exists, generic only over `TRequest, TResponse`, calls `IRateLimiter.CheckAsync` before invoking `next()`
- [x] Disallowed → short-circuits with `RateLimited` via `IOutcomeCaseRegistry`, carrying `LimitKey` and the limiter's `RetryAfter` through
- [x] Allowed → passes through `next()`'s result unchanged
- [x] Unit tests cover both branches, including `RetryAfter` propagation
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` passes
