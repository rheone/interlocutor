# 12 - InterlocutorBuilder DI wiring for all new/migrated behaviors

> **Triage note** - re-triaged from `ready-for-agent`, then resolved and moved back. The original draft asserted a new 8-behavior canonical order for `UseRecommendedChain()` (Cancellation → Logging → RateLimited → Timeout → Auditing → Authorization → Validation → Transaction) backed by one line of rationale, which would have changed the same ADR-0009-deliberated, shipped rationale (`UseRecommendedChain()`'s XML doc) without equivalent scrutiny. **Maintainer decision:** no prescribed order for the three new behaviors - ordering them is up to the consuming developer. `UseRecommendedChain()` stays exactly as it is today (the existing 5-behavior Logging → Auditing → Authorization → Validation → Transaction chain, unchanged); the three new `Add*Behavior()` methods are added as freestanding builder methods the consumer composes in whatever order they choose, same as any other non-recommended-chain behavior. Scope below updated accordingly.

## Labels

ready-for-agent

**What to build:** Wire everything from tickets 03/07/08/09/10/11 into the container-neutral `InterlocutorBuilder`: `IOutcomeCaseRegistry`'s default gets try-added inside `AddAuthorizationBehavior()` and `AddValidationBehavior()` (mirroring how those methods already try-add `IPolicyEvaluator`), and three new methods - `AddCancellationBehavior()`, `AddTimeoutBehavior()` (try-adds `ITimeoutPolicy` default), `AddRateLimitedBehavior()` (try-adds `IRateLimiter` default) - plus `OverrideTimeoutPolicy<T>()`, `OverrideRateLimiter<T>()`, `OverrideOutcomeCaseRegistry<T>()`. `UseRecommendedChain()` is **not** touched - it keeps its current 5-behavior order and XML doc unchanged. The three new methods are ordinary builder calls with no prescribed position; a consumer wires them into their own chain via plain `Add*Behavior()` call order, same as any behavior outside the recommended chain.

## Acceptance criteria

- [ ] `IOutcomeCaseRegistry` default (`DefaultOutcomeCaseRegistry`) is try-added inside `AddAuthorizationBehavior()` and `AddValidationBehavior()`
- [ ] `AddCancellationBehavior()`, `AddTimeoutBehavior()`, `AddRateLimitedBehavior()` exist on `InterlocutorBuilder`, each registering its behavior and try-adding its default evaluator (and `IOutcomeCaseRegistry` default)
- [ ] `OverrideTimeoutPolicy<T>()`, `OverrideRateLimiter<T>()`, `OverrideOutcomeCaseRegistry<T>()` exist, following the existing `Override*` pattern (e.g. `OverridePolicyEvaluator<T>()`)
- [ ] `UseRecommendedChain()` is unchanged - still exactly the 5-behavior Logging → Auditing → Authorization → Validation → Transaction chain, same XML doc
- [ ] Each new `Add*Behavior()` method's XML doc states it has no prescribed position and the caller chooses where it lands, consistent with the maintainer decision above
- [ ] DI resolution smoke tests for **both** `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.DependencyInjection.Autofac`: register each new/migrated behavior via `InterlocutorBuilder` and assert `IPipelineBehavior<TRequest,TResponse>` resolves without throwing (this is the exact failure mode described in `AuthorizationBehavior`'s remarks about Microsoft DI's open-generic resolver)
- [ ] `dotnet test` passes for `Interlocutor.DependencyInjection.Abstractions`, `.Microsoft`, and `.Autofac` test projects

## Blocked by

None - can start immediately
