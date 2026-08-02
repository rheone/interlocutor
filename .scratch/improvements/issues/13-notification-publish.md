> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed types; see remediation 14.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Implement a notification/event broadcasting mechanism — `INotification`, `INotificationHandler<TNotification>`, and `IMediator.Publish(...)`.

This is the biggest structural gap in the current design. There is no way to raise domain events (e.g., `OrderCancelled`) and have multiple independent handlers react to them.

The design should include:

```csharp
// Mediance.CommonKernel
public interface INotification;
// or abstract record — decide based on whether notifications need payload structure

// Mediance.Abstractions
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken ct);
}
```

And extend `IMediator` with:

```csharp
Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
    where TNotification : INotification;
```

Key design decisions:
- **Sequential vs parallel publication**: Notifications may be dispatched to handlers sequentially (ordered) or in parallel (fire-and-forget). Consider supporting both modes via a `PublishStrategy` enum.
- **Exception handling**: Should one handler's failure prevent others from running? Sequential mode should stop on failure; parallel mode should aggregate exceptions.
- **No return value**: Notifications should have no result — they are fire-and-forget. No `Result<T>` return.
- **Handler registration**: `INotificationHandler<T>` must be scannable by the assembly-scanning DI extensions.

The `Publish` method should be added to `IMediator` and implemented in `Mediator`. Consider an `IPublisher` interface for interface segregation (see ticket 14).

## Acceptance criteria

- [ ] `INotification` marker type defined
- [ ] `INotificationHandler<TNotification>` handler contract defined
- [ ] `IMediator.Publish<TNotification>` added and implemented
- [ ] Sequential and parallel publish strategies supported
- [ ] Assembly scanning registers `INotificationHandler<T>` implementations
- [ ] Tests cover: single handler, multiple handlers, sequential stop-on-failure, parallel aggregation
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
