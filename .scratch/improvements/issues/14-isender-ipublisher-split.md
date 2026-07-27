## Labels

ready-for-agent

## What to build

Split `IMediator` into `ISender` and `IPublisher` interfaces following the Interface Segregation Principle. The current `IMediator` combines both send (request/response) and publish (fire-and-forget) concerns.

Proposed split:

```csharp
// Mediance.Abstractions

public interface ISender
{
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
    Task<Result> Send(ICommand command, CancellationToken ct = default);
}

public interface IPublisher
{
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}

// IMediator remains as a combined convenience interface
public interface IMediator : ISender, IPublisher;
```

This allows consumers to depend on only the narrow interface they need:
- Command handlers that only send other commands depend on `ISender`
- Domain event handlers that only publish events depend on `IPublisher`
- Root-level code (controllers, background jobs) depends on `IMediator` for convenience

The concrete `Mediator` class should implement `IMediator` (which inherits both).

## Acceptance criteria

- [ ] `ISender` and `IPublisher` interfaces defined
- [ ] `IMediator` inherits both `ISender` and `IPublisher`
- [ ] DI registration registers `ISender`, `IPublisher`, and `IMediator`
- [ ] All existing call sites still compile and work
- [ ] All existing tests still pass

## Blocked by

- 13-notification-publish (so IPublisher has something to publish)
