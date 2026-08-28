> **Status: done** - merged in `7c80f85`.

# 10 - TimeoutBehavior + IHasTimeout + ITimeoutPolicy

## Labels

ready-for-agent

**What to build:** A new opt-in pipeline behavior producing the `Timeout` outcome case, mirroring `AuthorizationBehavior`'s marker+evaluator shape. `IHasTimeout` is a marker interface a request implements to declare `TimeSpan Budget`. `ITimeoutPolicy` (with `DefaultTimeoutPolicy` returning the request's own `Budget` unchanged, and `DelegateTimeoutPolicy` for inline configuration, mirroring `IPolicyEvaluator`/`DefaultPolicyEvaluator`/`DelegatePolicyEvaluator`) lets a consumer override the effective budget. `TimeoutBehavior<TRequest, TResponse>` races `next()` against `Task.Delay(budget, ct)`.

Document plainly (XML remarks, matching `AuthorizationBehavior`'s style of documenting real constraints) that: (1) since `IPipelineBehavior<TRequest,TResponse>.Handle`'s `next` is `Func<Task<TResponse>>` with no way to pass a shorter linked token, this behavior can only *report* a timeout, not actually abort the in-flight handler - the abandoned `next()` task keeps running in the background; (2) if the delay task "wins" because the ambient `ct` was cancelled rather than because the budget elapsed, this must propagate as `OperationCanceledException` (so `CancellationBehavior`, sitting outside it in the canonical order, converts it to `Cancelled` correctly) rather than being reported as `Timeout`.

**Blocked by:** 03 (IOutcomeCaseRegistry extension-point), 05 (new case types)

- [x] `IHasTimeout` marker interface (`TimeSpan Budget { get; }`) exists in `Interlocutor.Abstractions.Markers`
- [x] `ITimeoutPolicy` (`TimeSpan ResolveBudget(IHasTimeout request)`), `DefaultTimeoutPolicy`, `DelegateTimeoutPolicy` exist in `Interlocutor.Abstractions.Pipeline`
- [x] `TimeoutBehavior<TRequest, TResponse>` exists, generic only over `TRequest, TResponse`, races `next()` vs a delay for the resolved budget
- [x] Budget exceeded (delay wins, `ct` not cancelled) → short-circuits with `Timeout` via `IOutcomeCaseRegistry`, carrying `Budget` and actual `Elapsed`
- [x] Ambient cancellation during the race (delay wins because `ct.IsCancellationRequested`) → propagates as `OperationCanceledException`, not `Timeout`
- [x] Budget not exceeded → passes through `next()`'s result unchanged
- [x] XML doc remarks explicitly state the "cannot actually cancel downstream work" limitation
- [x] Unit tests cover all three branches above
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` passes
