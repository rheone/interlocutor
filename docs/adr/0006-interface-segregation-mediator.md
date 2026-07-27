# ADR-0006: Interface Segregation for Mediator

**Status:** Accepted

## Context

The mediator exposes two fundamentally different operations: request/response dispatch (`Send`) and fire-and-forget event broadcast (`Publish`). Consumers that only send queries or commands should not depend on the publishing interface, and consumers that only publish notifications should not depend on the send interface. A single monolithic `IMediator` interface forces unnecessary coupling.

## Decision

Split into three interfaces following the Interface Segregation Principle:

- **`ISender`** — Narrow interface for send-only. Exposes three `Send` overloads: `IQuery<TResponse>`, `ICommand<TResponse>`, and `ICommand` (void).
- **`IPublisher`** — Narrow interface for publish-only. Exposes `Publish<TNotification>(TNotification, CancellationToken)`.
- **`IMediator`** — Combined convenience interface that inherits both `ISender` and `IPublisher`. Used by root-level code (controllers, background jobs) that needs both capabilities.

DI registers `IMediator` (scoped), and then aliases `ISender` and `IPublisher` to the same `Mediator` instance:
```csharp
services.AddScoped<IMediator, Mediator>();
services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());
```

## Consequences

- **Positive:** Consumers declare only the interface they need — `ISender` for controllers, `IPublisher` for domain event emitters.
- **Positive:** Test doubles are simpler — mock only `ISender` or `IPublisher` rather than the full `IMediator`.
- **Positive:** The `IMediator` convenience interface preserves a familiar entry point for code that needs both.
- **Negative:** Three interfaces to maintain instead of one. The maintenance cost is negligible since `IMediator` explicitly inherits the other two and adds no members.

## Alternatives considered

- **Single `IMediator` interface:** Rejected — violates ISP, forces unnecessary dependencies on consumers that only send or only publish.
- **Separate `IMediator` with `ISender` and `IPublisher` as unrelated types:** Rejected — the common case of needing both would require depending on two interfaces, adding friction.
