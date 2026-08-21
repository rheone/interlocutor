# ADR-0007: Notification Publish Strategies

**Status:** Accepted (amended)

## Context

Notifications (`INotification`) are fire-and-forget domain events that can have zero, one, or many `INotificationHandler<T>` implementations. Handlers for the same notification may be independent (can run concurrently) or may have ordering requirements. The publish mechanism must support both scenarios.

Additionally, handlers can throw exceptions. The handling strategy determines whether a failure in one handler blocks other handlers from running and how exceptions are reported.

## Decision

Support two `PublishStrategy` values:

- **`Sequential`** (default): Handlers execute one by one. If a handler throws, remaining handlers are not invoked and the exception propagates to the caller unwrapped - the safe default, in that it makes failure behavior predictable, at the cost of leaving partial execution reachable: some handlers may have run and others not.
- **`Parallel`**: Handlers execute concurrently via `Task.WhenAll`. Every handler runs regardless of others failing; if multiple handlers throw, the exceptions are aggregated. Use when handlers are known to be independent and order does not matter.

**Ordering is explicitly unspecified**, not registration order. A handler registered against a base event type or interface receives every derived event, which makes "registration order" meaningless as soon as fan-out crosses handler families - there is no single registration list to be ordered against. Fan-out to a base type or interface is achieved by resolving handlers registered at every level of the published notification's type hierarchy that is itself assignable to `INotification` (its base types and implemented interfaces), then relying on `INotificationHandler<in TNotification>`'s own contravariance to treat a handler registered against a base type as an `INotificationHandler<TDerived>` - never by asking the container to match variance itself, since the built-in containers resolve closed generic services by exact type only.

The escape hatch for the rare genuine ordering need is relative, not numeric: `NotificationHandlerRunAfterAttribute`/`NotificationHandlerRunBeforeAttribute` name another handler type to run after or before, topologically sorted at build time into a generated order table, with a cycle reported as a build error (`INTERLOC004`) naming every handler type the cycle involves. (Numeric ordering values were considered and rejected - see Alternatives.) A catch-all handler implementing `INotificationHandler<INotification>` - subscribing to the base notification interface itself - is legal and produces an informational build diagnostic (`INTERLOC003`) naming it, so subscribing to everything is a deliberate choice rather than an accident.

The publish method defaults to `Sequential` when no strategy is specified:
```csharp
public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
    where TNotification : INotification
{
    return PublishAsync(notification, PublishStrategy.Sequential, ct);
}
```

The mediator caches separate `NotificationHandlerWrapper` instances per strategy - `SequentialNotificationHandlerWrapper` (foreach loop) and `ParallelNotificationHandlerWrapper` (linq `Select` + `Task.WhenAll`). Both overloads - the default-strategy one and the one taking a `PublishStrategy` explicitly - live on `IPublisher` itself, not only on the concrete `Mediator`, so a consumer coding against `IPublisher` can select the strategy without depending on `IMediator` or the implementation type.

`Interlocutor.Generator` computes the run-after/run-before topological order once per compilation and emits it as a generated `IGeneratedNotificationHandlerOrder` implementation, registered alongside the rest of a compilation's generated registrations exactly like the generated dispatch switch (ADR-0008) - resolved by the publisher at dispatch time and consulted only for handler types that were actually named in a run-after/run-before relationship; every other handler stays unranked and keeps its otherwise-unspecified resolution order.

## Consequences

- **Positive:** Sequential by default is predictable - a failure stops the run at a known point instead of continuing past it. Documentation states plainly that partial execution is still reachable: earlier handlers ran, the failing one and everything after it did not.
- **Positive:** Parallel is available for performance-sensitive independent handlers, and every handler is guaranteed to run regardless of others failing.
- **Positive:** Caching per-strategy avoids repeated reflection.
- **Positive:** Unspecified ordering plus a compiler-checked relative escape hatch means the common case (independent handlers) needs no ceremony, and the rare real ordering need is expressible and verified at build time rather than discovered by accident.
- **Negative:** Sequential execution means a slow or failing handler delays or prevents subsequent handlers. Mitigated by making `Parallel` an explicit opt-in for handlers that don't depend on each other.

## Alternatives considered

- **Fire-and-forget with no exception propagation:** Rejected - silent failures make debugging difficult. If handlers need true fire-and-forget semantics, the caller should use `Task.Run` or a background queue.
- **Only sequential:** Rejected - locks consumers into serial execution even when handlers are independent, which can become a bottleneck for high-volume events.
- **Background dispatch via channel/queue:** Rejected for the base library - belongs at the application layer where delivery guarantees are designed per-scenario.
- **Numeric ordering values:** Rejected as the relative-ordering mechanism - a global integer scale invites collisions and mid-range insertions as handlers accrete, and says nothing about *why* one handler must run before another. A named "runs after/before this handler type" attribute is self-documenting and only constrains the pairs that actually need it.
- **Registration order as the implicit ordering:** Rejected - once a handler can be registered against a base event type or interface and receive every derived event, "registration order" no longer names a single well-defined sequence to depend on.
