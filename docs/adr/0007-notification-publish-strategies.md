# ADR-0007: Notification Publish Strategies

**Status:** Accepted

## Context

Notifications (`INotification`) are fire-and-forget domain events that can have zero, one, or many `INotificationHandler<T>` implementations. Handlers for the same notification may be independent (can run concurrently) or may have ordering requirements. The publish mechanism must support both scenarios.

Additionally, handlers can throw exceptions. The handling strategy determines whether a failure in one handler blocks other handlers from running and how exceptions are reported.

## Decision

Support two `PublishStrategy` values:

- **`Sequential`** (default): Handlers execute one by one in registration order. If a handler throws, remaining handlers are not invoked and the exception propagates to the caller. This is the safe default — it makes failure behavior predictable and avoids partial-execution state where some handlers ran and others did not.
- **`Parallel`**: Handlers execute concurrently via `Task.WhenAll`. If multiple handlers throw, the exceptions are aggregated. Use when handlers are known to be independent and order does not matter.

The publish method defaults to `Sequential` when no strategy is specified:
```csharp
public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
    where TNotification : INotification
{
    return PublishAsync(notification, PublishStrategy.Sequential, ct);
}
```

The mediator caches separate `NotificationHandlerWrapper` instances per strategy — `SequentialNotificationHandlerWrapper` (foreach loop) and `ParallelNotificationHandlerWrapper` (linq `Select` + `Task.WhenAll`).

## Consequences

- **Positive:** Sequential by default is safe — no partial execution on failure.
- **Positive:** Parallel is available for performance-sensitive independent handlers.
- **Positive:** Caching per-strategy avoids repeated reflection.
- **Negative:** Sequential execution means a slow or failing handler delays or prevents subsequent handlers. Mitigated by making `Parallel` an explicit opt-in for handlers that don't depend on each other.

## Alternatives considered

- **Fire-and-forget with no exception propagation:** Rejected — silent failures make debugging difficult. If handlers need true fire-and-forget semantics, the caller should use `Task.Run` or a background queue.
- **Only sequential:** Rejected — locks consumers into serial execution even when handlers are independent, which can become a bottleneck for high-volume events.
- **Background dispatch via channel/queue:** Rejected for the base library — belongs at the application layer where delivery guarantees are designed per-scenario.
