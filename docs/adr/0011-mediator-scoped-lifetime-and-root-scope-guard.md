# ADR-0011: Mediator Scoped Lifetime, Non-Configurable, with a Root-Scope Guard

**Status:** Accepted

## Context

The mediator's lifetime determines what a handler, behavior, or stage can safely inject and how a unit of work (a transaction, a scoped `DbContext`/`ISession`) is shared across a single dispatch. If the mediator's own lifetime were configurable, the one genuinely dangerous setting - transient registered into a singleton consumer - is legal in every DI container and produces no error at registration time. A singleton hosted service that captures a transient-but-effectively-singleton mediator at startup silently resolves a scoped database context from the root provider and shares it across every request and every thread for the life of the process. In development, container scope validation may catch this and throw; in production, where scope validation defaults to off, it does not - it corrupts data silently.

## Decision

- **The three mediator interfaces (`IMediator`, `ISender`, `IPublisher`) are registered scoped, and the lifetime is not configurable.** There is no builder option to change it. Scoped specifically - not transient - because a scoped registration captured by a singleton is exactly the mistake most containers' own scope validation is built to catch; transient into singleton is legal everywhere and gives no such backstop.
- **The mediator never creates its own scope.** It borrows the ambient one it was resolved into. This is what makes the unit-of-work marker (`ITransactionalRequest`), transient dependencies, and disposables behave correctly within one dispatch - a handler, its behaviors, and its stages all see the same scoped instances. A handler that genuinely needs an independent scope injects the standard `IServiceScopeFactory` itself; the mediator does not do this on its behalf.
- **No service-locator API is exposed to handlers.** The mediator's service provider is an implementation detail used to resolve handlers, behaviors, and stages - it is never handed to handler code as a way to pull in arbitrary services.
- **A root-scope detector, implemented per container, throws a named exception when dispatch is attempted from the root provider.** The exception identifies the request type being dispatched and states the fix (resolve the mediator from a scope - a request scope, a created `IServiceScope`, or equivalent - rather than the root provider) so the failure is diagnosable without reading the library's source.

## Consequences

- **Positive:** The one incorrect lifetime setting is not a setting at all - it cannot be misconfigured because there is no configuration surface for it.
- **Positive:** A background service or hosted service that injects the mediator directly gets a loud, actionable exception instead of a silently shared root-scope database context.
- **Positive:** Handler, behavior, and stage constructor injection, transaction participation, and disposal all follow ordinary scoped-service rules - no special-casing to learn.
- **Negative:** A background service must explicitly create a scope per dispatch (`IServiceScopeFactory.CreateScope()`) rather than injecting the mediator once at construction - one more thing to know when writing that kind of consumer. Mitigated by the root-scope exception naming the fix directly.

## Alternatives considered

- **Configurable mediator lifetime:** Rejected outright - its only incorrect setting causes silent data corruption in production, where the safety net (container scope validation) is off by default. A configuration option whose wrong value is silent corruption is not a configuration option worth having.
- **Transient mediator lifetime:** Rejected - legal to capture in a singleton in every container, which is precisely the failure mode this decision exists to prevent. Scoped relies on the container's own scope-validation to surface the mistake instead of hiding it.
- **Mediator creates its own scope per dispatch:** Rejected - would break the unit-of-work model. A handler and its transaction behavior must share the caller's scoped `DbContext`/`ISession`; if the mediator opened a new scope internally, the handler's writes and the behavior's transaction would run against different instances.
