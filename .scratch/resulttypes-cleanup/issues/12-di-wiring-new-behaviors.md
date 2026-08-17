# 12 - InterlocutorBuilder DI wiring for all new/migrated behaviors

> **Triage note** - re-triaged from `ready-for-agent`. This ticket asserts a new 8-behavior canonical order (Cancellation → Logging → RateLimited → Timeout → Auditing → Authorization → Validation → Transaction) backed by one line of rationale in "What to build." The existing 5-behavior order got its own ADR-level deliberation (see [ADR-0009](../../../docs/adr/0009-no-default-pipeline.md)) explaining each position individually; this reordering changes that same documented, shipped rationale (`UseRecommendedChain()`'s XML doc) without the equivalent scrutiny. Confirm the order deliberately - or grill it - before moving this back to `ready-for-agent`; the rest of the ticket (the mechanical wiring itself) is otherwise fully specified.

## Labels

needs-triage

**What to build:** Wire everything from tickets 03/07/08/09/10/11 into the container-neutral `InterlocutorBuilder`: `IOutcomeCaseRegistry`'s default gets try-added inside `AddAuthorizationBehavior()` and `AddValidationBehavior()` (mirroring how those methods already try-add `IPolicyEvaluator`), and three new methods - `AddCancellationBehavior()`, `AddTimeoutBehavior()` (try-adds `ITimeoutPolicy` default), `AddRateLimitedBehavior()` (try-adds `IRateLimiter` default) - plus `OverrideTimeoutPolicy<T>()`, `OverrideRateLimiter<T>()`, `OverrideOutcomeCaseRegistry<T>()`. Update `UseRecommendedChain()` to the new canonical order: Cancellation (outermost) → Logging → RateLimited → Timeout → Auditing → Authorization → Validation → Transaction, with its XML-doc rationale updated to explain the new positions (throttled/past-budget requests short-circuit before paying for auditing/authorization/validation/a transaction).

**Blocked by:** 03 (IOutcomeCaseRegistry), 07 (AuthorizationBehavior migration), 08 (ValidationBehavior migration), 09 (CancellationBehavior), 10 (TimeoutBehavior), 11 (RateLimitedBehavior)

- [ ] `IOutcomeCaseRegistry` default (`DefaultOutcomeCaseRegistry`) is try-added inside `AddAuthorizationBehavior()` and `AddValidationBehavior()`
- [ ] `AddCancellationBehavior()`, `AddTimeoutBehavior()`, `AddRateLimitedBehavior()` exist on `InterlocutorBuilder`, each registering its behavior and try-adding its default evaluator (and `IOutcomeCaseRegistry` default)
- [ ] `OverrideTimeoutPolicy<T>()`, `OverrideRateLimiter<T>()`, `OverrideOutcomeCaseRegistry<T>()` exist, following the existing `Override*` pattern (e.g. `OverridePolicyEvaluator<T>()`)
- [ ] `UseRecommendedChain()` registers behaviors in the new order and its XML-doc rationale is updated
- [ ] A consumer can still override the order simply by calling the individual `Add*Behavior()` methods in a different sequence instead of `UseRecommendedChain()` - no new ordering API is introduced
- [ ] DI resolution smoke tests for **both** `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.DependencyInjection.Autofac`: register each new/migrated behavior via `InterlocutorBuilder` and assert `IPipelineBehavior<TRequest,TResponse>` resolves without throwing (this is the exact failure mode described in `AuthorizationBehavior`'s remarks about Microsoft DI's open-generic resolver)
- [ ] `dotnet test` passes for `Interlocutor.DependencyInjection.Abstractions`, `.Microsoft`, and `.Autofac` test projects
