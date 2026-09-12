# 03 - Conditional (canary/percentage-rollout) behavior wrapper

**Status:** ready-for-agent

**What to build:** A way to gate an existing `IPipelineBehavior<TRequest,TResponse>` behind a runtime predicate, so a new or risky behavior (a stricter validation rule, a new authorization policy) can be turned on for a fraction of traffic and ramped up without a redeploy, instead of the current all-or-nothing "it's in the chain or it isn't" choice every `Add*Behavior` call makes today.

**Scope:** an on/off or percentage gate evaluated per-dispatch. **Not** in scope: true shadow/observe-only execution (running the inner behavior but discarding its effect on the outcome) - that has no generic semantics for a behavior that short-circuits or transforms the response, and is a fast-follow only if real usage demands it (see the PRD's Non-Goals).

Build this from existing primitives rather than new registration plumbing - `InterlocutorFactoryRegistration` and the service-resolving `AddBehavior<TRequest,TResponse>(Func<IServiceProvider,...>, ...)` overload already establish the pattern this reuses:

```csharp
namespace Interlocutor.Abstractions.Pipeline;

public sealed class ConditionalBehavior<TRequest, TResponse>(
    IPipelineBehavior<TRequest, TResponse> inner,
    Func<TRequest, bool> shouldRun
) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        shouldRun(request) ? inner.HandleAsync(request, next, ct) : next();
}
```

Builder surface, on `InterlocutorPipelineBuilder`:

```csharp
public InterlocutorPipelineBuilder AddConditionalBehavior<TBehavior>(
    Func<IServiceProvider, TRequest, bool> shouldRun,
    RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
    // resolves TBehavior as `inner` via the container, wraps it in ConditionalBehavior<,>
```

(Exact generic shape - whether this needs to be per closed `TRequest,TResponse` like the delegate `AddBehavior` overloads, or can stay open-generic over `TBehavior` - is an open design question the implementer should resolve against how `AddBehavior`'s existing overloads already handle this; don't invent a third shape.)

Ship one built-in gate, a percentage rollout, config-bound per the PRD's split - the POCO lives in the container-neutral core, `IOptionsMonitor` binding is Microsoft-DI-specific sugar:

```csharp
namespace Interlocutor.Abstractions.Pipeline;

public sealed class RolloutOptions
{
    public double PercentageEnabled { get; set; } // 0-100
}
```

A default gate implementation evaluates `PercentageEnabled` via a simple random draw per dispatch (no sticky-by-key assignment for v1 - note that as a fast-follow, not required now, to keep this scoped). Under Microsoft DI, the gate resolves `IOptionsMonitor<RolloutOptions>` (keyed by name, so more than one canary can run concurrently - use the standard named-options pattern, e.g. `services.Configure<RolloutOptions>("MyNewValidationRollout", configuration.GetSection(...))`) so `PercentageEnabled` can be adjusted live via config reload without a redeploy.

**Acceptance criteria:**
- [x] `ConditionalBehavior<TRequest,TResponse>` runs `inner` only when `shouldRun(request)` is true, otherwise passes straight to `next()`
- [x] `AddConditionalBehavior` registers the wrapped behavior resolvably at the correct position in the chain, consistent with how an ordinary `AddBehavior` call would
- [x] The shipped percentage-rollout gate, at 0%, never runs `inner`; at 100%, always runs it; at an intermediate value, runs it approximately that fraction of the time (statistical test with a tolerance, not an exact count)
- [x] Under Microsoft DI, `RolloutOptions.PercentageEnabled` is adjustable via `IOptionsMonitor<RolloutOptions>` reload without restarting the application
- [x] Autofac gets the identical POCO-configured gate with no `IOptions` dependency (a plain `Func<TRequest, bool>` or an injected non-reloading `RolloutOptions` snapshot is an acceptable Autofac-side substitute - document the difference in capability rather than silently degrading)
- [x] `documentation/pipeline-and-behaviors.md` documents this with a worked example (e.g., ramping a new validation rule from 0% to 100%)
- [x] New tests cover: gate on/off, percentage distribution, live reload under Microsoft DI

**Blocked by:** None - can start immediately.
