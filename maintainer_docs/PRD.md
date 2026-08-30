# PRD: Interlocutor - Mediator Result Pipeline for .NET

**Status:** Active
**Target platform:** .NET 11 / C# 15 (union types, preview - `EnablePreviewFeatures`)

---

## 1. Summary

Interlocutor is a mediator/CQRS pipeline library for .NET built on C# 15 union types. Handlers return a closed, compiler-checked `OperationOutcome`/`OperationOutcome<...>` union instead of throwing exceptions for expected outcomes (not found, forbidden, validation failure, conflict). Cross-cutting concerns compose around handlers via `IPipelineBehavior<TRequest, TResponse>` in an onion/Russian-doll model, empty by default. Exceptions remain reserved for bugs and infrastructure faults.

## 2. Problem statement

Before this library, "what can go wrong" for a given handler was implicit - discoverable only by reading the method body, not the signature. Failure paths were inconsistently represented (exceptions, null returns, boolean flags), making API-layer error mapping ad hoc, testing incomplete, and cross-cutting concerns hard to compose.

## 3. Goals

- Every possible outcome visible in the type signature and exhaustively checked by the compiler.
- Exceptions reserved for bugs and infrastructure failures only.
- One consistent mapping from domain outcome to HTTP response.
- Cross-cutting concerns composed via pipeline behaviors without shared mutable state.
- One transactional session per request, guaranteed by DI scope lifetime.

## 4. Non-goals

- Replacing ASP.NET Core's exception-handling middleware.
- Infrastructure retries/circuit breakers in the mediator pipeline.
- Streaming responses (`IAsyncEnumerable<T>` or similar) - deliberately excluded, not merely unaddressed (ADR-0013).
- Native AOT support today - trim-compatible only (ADR-0016).

## 5. Package structure

| Package | Contents | Depends on |
|---|---|---|
| `Interlocutor.CommonKernel` | `OperationOutcome`/`OperationOutcome<...>` (2-6 arities), `Success`, `Failure`/`Failures`, `NotFound`, `Forbidden`, `Conflict`/`Conflicts`, `ValidationError`/`ValidationErrors` | .NET BCL only |
| `Interlocutor.Abstractions` | `IMediator`/`ISender`/`IPublisher`, handler contracts, marker interfaces, `Mediator`, `ResourceCommandHandler`, `InterlocutorStartupValidation`, built-in pipeline behaviors (Logging, Authorization, Auditing, Cancellation, Timeout, RateLimited, PipelineStage), `IOutcomeCaseRegistry`/`IOutcomeClassifier` | `Interlocutor.CommonKernel` |
| `Interlocutor.Generator` | Roslyn incremental source generator behind compile-time handler discovery (ADR-0008); analyzer reference only, never a runtime dependency | - |
| `Interlocutor.AspNetCore` | `ToActionResult()`/`ToMinimalApiResult()`, `IOutcomeStatusMapper`/`DefaultOutcomeStatusMapper` | `Interlocutor.CommonKernel`, `Interlocutor.Abstractions`, ASP.NET Core |
| `Interlocutor.DependencyInjection.Abstractions` | Container-neutral registration model - `InterlocutorBuilder`, `InterlocutorPipelineBuilder`, `RegistrationReport` | `Interlocutor.Abstractions` |
| `Interlocutor.DependencyInjection.Microsoft` | `AddInterlocutor()` for `IServiceCollection` | `Interlocutor.DependencyInjection.Abstractions`, `Interlocutor.Persistence.Abstractions` |
| `Interlocutor.DependencyInjection.Autofac` | `AddInterlocutor()` for `ContainerBuilder` | `Interlocutor.DependencyInjection.Abstractions`, `Interlocutor.Persistence.Abstractions` |
| `Interlocutor.FluentValidation` | `ValidationBehavior` | `Interlocutor.Abstractions` |
| `Interlocutor.Persistence.Abstractions` | `IUnitOfWork`, `IDomainRepository<TEntity, TKey>`, `Page<T>`, `SortCriterion<TEntity>` - zero ORM dependency | `Interlocutor.Abstractions` |
| `Interlocutor.Persistence.EntityFrameworkCore` | `EntityFrameworkUnitOfWork<TContext>`, `EntityFrameworkRepository<TEntity, TKey, TContext>`, `UnitOfWorkBehavior<,,>` | `Interlocutor.Persistence.Abstractions` |
| `Interlocutor.Persistence.NHibernate` | `NHibernateUnitOfWork`, `NHibernateRepository<TEntity, TKey>`, `UnitOfWorkBehavior<,,>` | `Interlocutor.Persistence.Abstractions` |

`Interlocutor.EntityFrameworkCore` and `Interlocutor.NHibernate` (singular `TransactionBehavior`, no persistence abstraction) are retired - superseded by the `Interlocutor.Persistence.*` packages above (ADR-0017, ADR-0018).

## 6. Type catalog

| Type | Kind | Purpose |
|---|---|---|
| `Success` | `readonly record struct` | Marks a no-payload command success |
| `Failure` / `Failure<TErrorCode>` | `readonly record struct` | Generic fallback failure - reason-only, or reason + machine-readable error code |
| `NotFound` / `NotFound<TId>` | `readonly record struct` | Entity does not exist |
| `Forbidden` | `readonly record struct` | Authorization denied |
| `Conflict` | `readonly record struct` | State conflict (concurrency, uniqueness, invalid transition) - `Message` and `Code` both mandatory |
| `ValidationError` / `ValidationError<TErrorCode>` | `readonly record struct` | A single validation failure |
| `ValidationErrors` / `ValidationErrors<TErrorCode>` | `readonly record struct` | De-duplicated aggregate of `ValidationError` instances |
| `Cancelled`, `Timeout`, `RateLimited` | `readonly record struct` | Cross-cutting opt-in behavior outcomes |
| `OperationOutcome` | `union` | `union OperationOutcome(Success, Failure)` - void command outcome |
| `OperationOutcome<TSuccess, TFailure...>` | `union` | Generic in 2-6 case types; first is success, the rest independent failure cases |

There is no closed `Failure` union of sub-cases - every failure type above is an independent case type usable directly in an `OperationOutcome<...>`'s type argument list, per the operation's own choice of arity (see [documentation/composing-operationoutcome-arities.md](../documentation/composing-operationoutcome-arities.md)).

## 7. CQRS interfaces

| Interface | Purpose |
|---|---|
| `IQuery<TResponse>` | Marker for a read request; `TResponse : IOperationOutcome` |
| `ICommand<TResponse>` | Marker for a value-returning write request; `TResponse : IOperationOutcome` |
| `ICommand` | Marker for a unit (no-payload) write request - the outcome type is chosen by the *caller*, via `ISender.Send<TResult>(ICommand, ...)`'s type argument, not fixed by the command |

There are no shipped `IEnumerableQuery<T>`, `IBulkCommand<T>`, or `IPagedQuery<T>` marker interfaces. A query or command that returns a list, a paged result, or does per-item bulk validation is expressed with the three markers above plus a consumer-defined response type - see `documentation/glossary.md`'s `PagedResult<T>` entry.

## 8. Handler contracts

| Interface | Purpose |
|---|---|
| `IQueryHandler<TQuery, TResponse>` | Handles `IQuery<TResponse>` |
| `ICommandHandler<TCommand, TResponse>` | Handles `ICommand<TResponse>` |
| `IVoidCommandHandler<TCommand, TResult>` | Handles `ICommand`, paired with whichever outcome type the call site requests |
| `ResourceCommandHandler<TCommand, TEntity, TSuccess>` | Template method: load → authorize → handle; returns `OperationOutcome<TSuccess, NotFound, Failure>` |
| `INotificationHandler<TNotification>` | Handles `INotification` (fire-and-forget, contravariant fan-out - more than one handler is normal) |

## 9. Pipeline behaviors

All behaviors implement `IPipelineBehavior<TRequest, TResponse>` and compose via a `next()` delegate; first registered wraps outermost.

| Behavior | Package | Produces | Opt-in via |
|---|---|---|---|
| `LoggingBehavior` | `Interlocutor.Abstractions` | - | Explicit registration; no marker |
| `AuditingBehavior` | `Interlocutor.Abstractions` | - | `IAuditableRequest` + `IAuditSink` |
| `AuthorizationBehavior` | `Interlocutor.Abstractions` | `Forbidden` | `IRequiresPolicy` + `IPolicyEvaluator` |
| `CancellationBehavior` | `Interlocutor.Abstractions` | `Cancelled` | `ICancellable` |
| `TimeoutBehavior` | `Interlocutor.Abstractions` | `Timeout` | `IHasTimeout` + `ITimeoutPolicy` |
| `RateLimitedBehavior` | `Interlocutor.Abstractions` | `RateLimited` | `IRateLimited` + `IRateLimiter` |
| `PipelineStageBehavior` | `Interlocutor.Abstractions` | - | Aggregates registered `IPipelineStage<,>` pre/post hooks |
| `ValidationBehavior` | `Interlocutor.FluentValidation` | `ValidationError(s)` | No marker - validates every request, if referenced |
| `UnitOfWorkBehavior<,,>` | `Interlocutor.Persistence.EntityFrameworkCore` / `.NHibernate` | - | No marker required; `ITransactionalRequest`/`IExternalStoreRequest` narrow it |

### 9.1 Execution order

Nothing runs by default (ADR-0009) - registration order is execution order. The documented recommended chain (`UseRecommendedChain()`) is Logging → Auditing → Authorization → Validation → Transaction, opt-in sugar with its own rationale, not a default. `Cancellation`/`Timeout`/`RateLimited` behaviors have no prescribed position; a consumer composes them wherever suits the application.

### 9.2 Transaction behavior

- `UnitOfWorkBehavior<TRequest, TResponse, TContext>` uses `Lazy<TContext>`/`Lazy<ISession>` to defer session construction; a transaction opens lazily on first write, or eagerly at pipeline start for `ITransactionalRequest`.
- `IExternalStoreRequest` declares "no SQL session" - `UnitOfWorkBehavior` never opens a transaction for it.
- Commits on success; rolls back on any failure case, or `IUnitOfWork.Cancel()`.
- `IUnitOfWork` *is* the transaction owner, not a façade - see [documentation/transactions-and-store-integration.md](../documentation/transactions-and-store-integration.md).

### 9.3 Void command pipeline

`ICommand` dispatches through the same pipeline mechanism as typed commands, resolved against the specific `IVoidCommandHandler<TCommand, TResult>` the call site's `Send<TResult>` type argument names (ADR-0005).

### 9.4 Marker interfaces for cross-cutting opt-in

| Interface | Opts into | Carries |
|---|---|---|
| `ITransactionalRequest` | Eager transaction begin (every write already gets one automatically) | - |
| `IExternalStoreRequest` | Declares no SQL session needed | - |
| `IAuditableRequest` | `AuditingBehavior` | `ActionName` |
| `IRequiresPolicy` | `AuthorizationBehavior` | `PolicyName` |
| `ICancellable` | `CancellationBehavior` | - |
| `IHasTimeout` | `TimeoutBehavior` | `Budget` |
| `IRateLimited` | `RateLimitedBehavior` | `LimitKey` |

### 9.5 Resource-based authorization

Needing the loaded entity is handled by `ResourceCommandHandler<TCommand, TEntity, TSuccess>`'s template method, not a pipeline behavior - it runs after the handler has already started, not as an outer behavior. Sequence: `LoadAsync` → `Authorize` (returns `Failure?`) → `HandleAsync`.

## 10. Mediator

The `Mediator` class implements `IMediator` (which inherits `ISender` + `IPublisher`):

- **`ISender`** (`Send`): the typed overloads (`IQuery<TResponse>`, `ICommand<TResponse>`, `ICommand` + a `TResult` type argument) resolve the handler directly from DI and run it through the pipeline - handler discovery itself is compile-time (ADR-0008), not reflective. A separate runtime-typed `Send(object, ct)` overload exists for infrastructure code (outbox/inbox, a queue consumer) that doesn't know the request type statically; it dispatches through a compile-time generated type switch (`IGeneratedRequestDispatcher`) when generated registrations are used, falling back to reflection otherwise.
- **`IPublisher`** (`Publish`): dispatches notifications to every applicable `INotificationHandler<T>`, `Sequential` (default) or `Parallel`.

The mediator resolves all handlers from DI - it has no internal handler registry (ADR-0003, superseded by ADR-0008's compile-time discovery).

## 11. DI registration

Both `Interlocutor.DependencyInjection.Microsoft` and `.Autofac` support the same `AddInterlocutor(Action<InterlocutorBuilder>)` delegate, translating a container-neutral `InterlocutorRegistration`/`InterlocutorFactoryRegistration` list into their own primitives. `IMediator`/`ISender`/`IPublisher` register scoped, wrapped in a `RootScopeGuardedMediator` that throws if resolved from the root container.

### 11.1 Startup validation

Nothing is validated automatically. Three independent, opt-in mechanisms exist: the source generator's `INTERLOC001` build diagnostic (missing handler, same compilation), `InterlocutorBuilder.ValidateOnBuild()` (checked against the accumulated registration list at `AddInterlocutor()` time, throws `InterlocutorRegistrationValidationException`), and `IServiceProvider.ValidateHandlerRegistrations(...)` (opt-in runtime check against a built container, throws `InvalidOperationException`). See [documentation/builder-and-registration.md § Startup Validation](../documentation/builder-and-registration.md#startup-validation).

## 12. HTTP mapping

`ResultExtensions.ToActionResult()`/`ToMinimalApiResult()` map each failure case to HTTP via `IOutcomeStatusMapper`:

| Failure case | HTTP Status | Body |
|---|---|---|
| `ValidationErrors` | 422 Unprocessable Entity | `ProblemDetails` (messages joined, no per-field dictionary) |
| `NotFound` | 404 Not Found | Message, or none |
| `Forbidden` | 403 Forbidden | None |
| `Conflict` | 409 Conflict | `ProblemDetails` |
| `Failure` / `Failures` | 400 Bad Request | `ProblemDetails` - deliberately not 500; a handler-chosen outcome, not a server fault |
| `Cancelled` | 499 Client Closed Request | None |
| `Timeout` | 408 Request Timeout | `ProblemDetails` |
| `RateLimited` | 429 Too Many Requests | `ProblemDetails` |
| Unrecognized custom type | 500 Internal Server Error | The failure value itself |

See [documentation/aspnetcore-integration.md](../documentation/aspnetcore-integration.md) for the full table and custom-mapping story.

## 13. Notifications

- `INotification` is a marker interface for domain events.
- `INotificationHandler<TNotification>` handles one notification type; contravariant fan-out means a handler for a base type also receives derived types.
- Multiple handlers for the same notification is normal and expected (unlike queries/commands, where it's a build diagnostic).
- `PublishStrategy.Sequential` (default): handlers run in order, stop on first exception.
- `PublishStrategy.Parallel`: handlers run concurrently, exceptions aggregated.

## 14. Ambient context

`IRequestContextAccessor` provides scoped ambient context (`RequestContext?`, null if unpopulated) carrying `CorrelationId` (non-nullable, auto-generated GUID default), `UserId`, `TenantId`, and `Items` (`IReadOnlyDictionary<string, object>?`). `RequestContext` is an immutable record; swap the reference via `Context { get; set; }`.

## 15. Architecture decisions

Design decisions and their rationale live in `maintainer_docs/adr/` as immutable, point-in-time records - see [README.md](README.md)'s theme-grouped index rather than duplicating them here. `bootstrapreference/` holds the pre-implementation bootstrap PRD/ADR this spec originally reconciled against; it's superseded and kept for historical context only.

## 16. Testing strategy

- Handlers: unit-testable directly (bypass the pipeline) for business logic.
- Pipeline: integration tests verify composition order, short-circuiting, and transaction lifecycle.
- DI: `tests/Interlocutor.Architecture.Tests` (NetArchTest) asserts the cross-package dependency rules in the root `CLAUDE.md`; opt-in startup validation catches missing handler registrations before first dispatch, not automatically at container build time.

## 17. Risks

- C# 15 union syntax is preview until .NET 11 GA (~November 2026). Usage is isolated to `Interlocutor.CommonKernel`'s result types.
- Marker interface opt-in has no compiler enforcement that a request implementing one marker also implements a related one it probably needs (e.g. a write command with no `ITransactionalRequest`/`IExternalStoreRequest` reasoning documented). Not currently covered by `tests/Interlocutor.Architecture.Tests`; a candidate for a future architecture test if this proves a real source of bugs.
- Struct-based unions box a value-type success/case payload at points where `IOperationOutcome`/`object` typing is required (`IOutcomeCaseRegistry`, runtime-typed dispatch). Acceptable for mediator dispatch; the underlying constraint is also what blocks Native AOT support today - see ADR-0016 for the full evidence.
