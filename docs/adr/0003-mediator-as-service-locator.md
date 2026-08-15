# ADR-0003: Mediator as Service Locator — No Internal Handler Registry

**Status:** Superseded by [ADR-0008](0008-compile-time-handler-discovery.md)

> **Superseded.** The container remains the handler registry — that half of this decision still holds. What changed is *how* the container learns which handlers exist: [ADR-0008](0008-compile-time-handler-discovery.md) replaces the runtime `IServiceProvider.GetRequiredService` resolution and assembly-scanning registration described below with source-generated registration modules and a generated dispatch switch, for build-time diagnostics and AOT compatibility rather than the startup-cost reason this ADR's alternatives section anticipated. "The container is the handler registry" and "handlers may locate services themselves" are different claims — this ADR's title has caused that conflation before. Only the first was ever true; the second was never true and is not what supersession changes.

## Context

MediatR maintains an internal dictionary mapping request types to handler types, resolved once and cached. This requires explicit registration or assembly scanning at startup. An alternative is to delegate handler resolution entirely to the DI container on every dispatch, letting the DI container manage lifetimes and avoiding a separate registration path.

## Decision

The `Mediator` class does not maintain its own handler registry. It resolves handlers from `IServiceProvider` at dispatch time:

- For queries and typed commands: wraps the handler resolution + pipeline composition in a cached `HandlerWrapper<TResponse>`, which calls `services.GetRequiredService<IQueryHandler<TQuery,TResponse>>()` (or `ICommandHandler<,>`) inside the `Handle` method.
- For void commands: `VoidCommandHandlerWrapper<TCommand>` resolves `ICommandHandler<TCommand>` and wraps the `Result` in a `Result<Result>` for pipeline compatibility.
- Pipeline behaviors are resolved via `services.GetServices<IPipelineBehavior<TRequest,TResponse>>()` on each dispatch.

The handler wrappers themselves are cached in `ConcurrentDictionary<Type, object>` to avoid repeated `Activator.CreateInstance` calls.

## Consequences

- **Positive:** No separate handler registration API — `AddInterlocutor()` assembly scanning handles everything uniformly. The DI container is the single source of truth for lifetimes.
- **Positive:** Handler lifetimes are controlled by DI registration (scoped by default), not by the mediator.
- **Negative:** Slight reflection overhead on first dispatch per request type — cached via `ConcurrentDictionary` so amortized to one cost per type.

## Alternatives considered

- **Explicit handler registry (MediatR pattern):** Requires users to register handlers in both DI and the mediator. Rejected as redundant — all information needed is already in DI.
- **Source-generated handler registration:** Would eliminate reflection entirely. Considered future optimization if startup reflection becomes a bottleneck.
