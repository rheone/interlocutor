# ADR-0010: Dispatch Scope — Notification Fan-Out, Open Generics, and Build-Error Ambiguity

**Status:** Accepted

## Context

Three separate dispatch questions need one consistent answer, because they all reduce to "given a request or notification instance, which handler(s) run": whether a query or command can match more than one handler, whether a notification can match more than one handler, and how a handler written against a generic abstraction interacts with a handler written against one concrete request.

Handler selection today also implicitly depends on things it should not: which namespace a handler lives in, or which assembly it was compiled into, can silently change which handler dispatch picks when more than one candidate exists.

## Decision

- **Queries and commands remain single-handler.** Contravariant fan-out applies to notification handlers only; a query or command dispatch always resolves to exactly one handler.
- **Notification fan-out is contravariant.** A handler registered against a base event type or interface receives every derived notification. This is a deliberate difference from query/command dispatch: notifications are fire-and-forget broadcast, not request/response, so more than one interested party is the normal case, not an ambiguity.
- **Open generic handlers are supported and closed by the generator.** A handler written against a generic abstraction (e.g. `IQueryHandler<GetById<TEntity>, TEntity>`) is closed by the source generator against the concrete request types it discovers in the compilation — the generator emits the closed registration and dispatch entry, the handler author never writes one file per concrete type.
- **Handler-serves-an-unrelated-base-class dispatch is not supported.** Only the request type's own generic shape participates in matching; there is no dispatch path based on inheriting from an unrelated abstraction the request type doesn't itself declare.
- **Selection rule, stated once:** a closed handler beats an open generic handler that also matches. Two open generic handlers matching the same request is a build error. There is no ranking by inheritance distance and no consideration of namespace or assembly location — moving a handler file between namespaces or projects never changes which handler runs.
- **Ambiguity is a build error, with an explicit override escape hatch.** An overrides attribute — living in the mediator abstractions module so a handler assembly takes no DI dependency to use it — is the sole mechanism to declare that one handler intentionally supersedes another. Without it, an ambiguous match fails the build rather than resolving silently.
- **Runtime-typed dispatch for infrastructure callers.** A dispatch overload accepts an `object` request (type known only at runtime — an outbox, inbox, or queue consumer deserializing off a message) and returns the library's base outcome interface, so generic infrastructure can inspect success/failure without knowing the concrete result shape. It is backed by a generated type switch into the strongly-typed dispatch path described in [ADR-0008](0008-compile-time-handler-discovery.md); a reflection-based implementation exists only under the explicit opt-in runtime-scanning fallback, never active by default.

## Consequences

- **Positive:** "Which handler runs" is answerable by reading generic constraints and the overrides attribute — never by knowing where a file happens to live.
- **Positive:** Ambiguity fails the build instead of resolving to whichever handler DI happened to register last.
- **Positive:** A handler family written once against a generic abstraction covers every concrete request in that family, without one boilerplate file per type.
- **Positive:** Runtime-typed dispatch gives outbox/inbox/queue infrastructure a supported path instead of forcing consumers to hand-roll reflection.
- **Negative:** The override attribute is one more thing a handler author must reach for on the (expected to be rare) occasion a concrete handler needs to beat a matching open generic one deliberately. Accepted — the alternative is silent, order-dependent resolution.

## Alternatives considered

- **Rank ambiguous matches by inheritance distance:** Rejected — implicit precedence rules are exactly the "surprising because you can't see it from the call site" behavior this whole effort removes. An explicit override attribute is one line and is visible in the code that declares it.
- **First-registered-wins for ambiguous matches:** Rejected — reintroduces the namespace/assembly/registration-order dependency this decision explicitly forbids.
- **Contravariant fan-out for commands and queries, matching notifications:** Rejected — request/response dispatch needs exactly one answer; more than one handler matching a command is a configuration mistake to report, not a feature to support.
