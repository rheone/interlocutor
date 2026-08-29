# PRD: Interlocutor - Mediator Result Pipeline for .NET

**Status:** Active
**Target platform:** .NET 11 / C# 15 (union types)
**Source:** Reconciles [bootstrap PRD](../bootstrapreference/PRD-mediator-result-pipeline.md) with the implemented source in `src/`.

---

## 1. Summary

Interlocutor is a mediator/CQRS pipeline library for .NET built on C# 15 union types. Handlers return a closed, compiler-checked `OperationOutcome<T>` or `OperationOutcome` type instead of throwing exceptions for expected outcomes (not found, forbidden, validation failure, conflict). Cross-cutting concerns compose around handlers via `IPipelineBehavior<TRequest,TResponse>` in an onion/Russian-doll model. Exceptions remain reserved for bugs and infrastructure faults.

## 2. Problem statement

Before this library, "what can go wrong" for a given handler was implicit - discoverable only by reading the method body, not the signature. Failure paths were inconsistently represented (exceptions, null returns, boolean flags), making API-layer error mapping ad hoc, testing incomplete, and cross-cutting concerns hard to compose.

## 3. Goals

- Every possible outcome visible in the type signature and exhaustively checked by the compiler.
- Exceptions reserved for bugs and infrastructure failures only.
- One consistent mapping from domain outcome to HTTP response.
- Cross-cutting concerns composed via pipeline behaviors without shared mutable state.
- One transactional session per request guaranteed by DI scope lifetime.

## 4. Non-goals

- Replacing ASP.NET exception-handling middleware.
- Infrastructure retries/circuit breakers in the mediator pipeline.
- Serializing `OperationOutcome`/`Failure` over the wire - API boundary always maps to a dedicated response shape.

## 5. Package structure

| Package | Contents | Dependencies |
|---------|----------|-------------|
| `Interlocutor.CommonKernel` | `OperationOutcome<T>`, `OperationOutcome`, `Failure`, `Success` - pure data types | .NET BCL only |
| `Interlocutor.Abstractions` | `IQuery<T>`, `ICommand<T>`, `ICommand`, marker interfaces, handler contracts, `IPipelineBehavior`, `IMediator`, `Mediator`, `ResourceCommandHandler`, `StartupValidation`, built-in pipeline behaviors (Logging, Auditing, Authorization, PipelineStage) | `Interlocutor.CommonKernel` |
| `Interlocutor.AspNetCore` | `ToActionResult()` HTTP mapping | `Interlocutor.CommonKernel`, ASP.NET Core |
| `Interlocutor.DependencyInjection.Microsoft` | Assembly-scanning `AddInterlocutor()` for MS DI | `Interlocutor.Abstractions` |
| `Interlocutor.DependencyInjection.Autofac` | Assembly-scanning `AddInterlocutor()` for Autofac | `Interlocutor.Abstractions` |
| `Interlocutor.EntityFrameworkCore` | `TransactionBehavior<TRequest,TResponse,TContext>` for EF Core | `Interlocutor.Abstractions` |
| `Interlocutor.NHibernate` | `TransactionBehavior<TRequest,TResponse>` for NHibernate | `Interlocutor.Abstractions` |
| `Interlocutor.FluentValidation` | `ValidationBehavior` | `Interlocutor.Abstractions` |

## 6. Type catalog

| Type | Kind | Definition | Purpose |
|------|------|------------|---------|
| `Success` | `record` | `record Success;` | Marks a no-payload command success |
| `ValidationFailed` | `record` | `record ValidationFailed(IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> Errors);` | Input validation failure |
| `RuleViolation` | `record` | `record RuleViolation(string Code, string Message);` | Business rule violation |
| `NotFound` | `record` | `record NotFound(string EntityType, object Key);` | Entity not found |
| `Forbidden` | `record` | `record Forbidden(string? Reason = null);` | Authorization denied |
| `Conflict` | `record` | `record Conflict(string Code, string Message);` | State conflict |
| `Failure` | `union` | `readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);` | Closed set of all failures |
| `OperationOutcome<T>` | `union` | `readonly union OperationOutcome<T>(T, Failure);` | Outcome of queries and value-returning commands |
| `OperationOutcome` | `union` | `readonly union OperationOutcome(Success, Failure);` | Outcome of void commands |

## 7. CQRS interfaces

| Interface | Purpose |
|-----------|---------|
| `IQuery<TResponse>` | Marker for queries returning `OperationOutcome<TResponse>` |
| `ICommand<TResponse>` | Marker for commands returning `OperationOutcome<TResponse>` |
| `ICommand` | Marker for void commands returning `OperationOutcome` |

There are no shipped `IEnumerableQuery<T>`, `IEnumerableCommand<T>`, `IBulkCommand<T, T>`, or `IPagedQuery<T>` marker interfaces. A query or command that returns a list, a paged result, or does per-item bulk validation is expressed with the three markers above plus a consumer-defined response type - see `documentation/glossary.md`'s `PagedResult<T>` entry for the paging shape as one example of that pattern.

## 8. Handler contracts

| Interface | Purpose |
|-----------|---------|
| `IQueryHandler<TQuery, TResponse>` | Handles `IQuery<TResponse>` |
| `ICommandHandler<TCommand, TResponse>` | Handles `ICommand<TResponse>` |
| `ICommandHandler<TCommand>` | Handles `ICommand` (void) |
| `ResourceCommandHandler<TCommand,TEntity,TResponse>` | Template method: load → authorize → handle |
| `INotificationHandler<TNotification>` | Handles `INotification` (fire-and-forget) |

## 9. Pipeline behaviors

All behaviors implement `IPipelineBehavior<TRequest,TResponse>` and compose via `next()` delegate in reverse registration order.

| Behavior | Package | Produces | Notes |
|----------|---------|----------|-------|
| `LoggingBehavior` | `Interlocutor.Abstractions` | - | Logs request type, handler, duration, outcome |
| `AuthorizationBehavior` | `Interlocutor.Abstractions` | `Forbidden` | Via `IRequiresPolicy` + `IPolicyEvaluator` |
| `AuditingBehavior` | `Interlocutor.Abstractions` | - | Via `IAuditableRequest` + `IAuditSink` |
| `PipelineStageBehavior` | `Interlocutor.Abstractions` | - | Aggregates `IPipelineStage` pre/post hooks |
| `ValidationBehavior` | `Interlocutor.FluentValidation` | `ValidationFailed` | FluentValidation, short-circuits before handler |
| `TransactionBehavior` (EF Core) | `Interlocutor.EntityFrameworkCore` | - | Commit on success, rollback on Failure |
| `TransactionBehavior` (NHibernate) | `Interlocutor.NHibernate` | - | Same semantics |

### 9.1 Execution order

Designed order: Logging → Auditing → Validation → Authorization → Transaction → Handler

This matches the intended order from the bootstrap PRD (see [bootstrap §7.1](../bootstrapreference/PRD-mediator-result-pipeline.md#71-request-flow-sequence)). Actual DI registration order determines the chain - behaviors are wrapped in reverse registration order, so the first registered behavior wraps outermost.

### 9.2 Transaction behavior

- `TransactionBehavior` uses `Lazy<TContext>`/`Lazy<ISession>` to defer session construction until a request implements `ITransactionalRequest`.
- `IExternalStoreRequest` positively declares "deliberately no SQL session."
- Commits on success; rolls back on `Failure`.
- See [bootstrap Addendum A.1](../bootstrapreference/PRD-mediator-result-pipeline.md#a1-non-database-backed-requests).

### 9.3 Void command pipeline

Void commands (`ICommand`) dispatch through the same pipeline mechanism as typed commands via `VoidCommandHandlerWrapper<TCommand>`, which wraps the void handler's `Task<OperationOutcome>` return as `Task<OperationOutcome<OperationOutcome>>` for pipeline compatibility, then unwraps.

### 9.4 Marker interfaces for cross-cutting opt-in

| Interface | Opts into | Carries |
|-----------|-----------|---------|
| `ITransactionalRequest` | TransactionBehavior | - |
| `IExternalStoreRequest` | Declares no SQL needed | - |
| `IAuditableRequest` | AuditingBehavior | `ActionName` |
| `IRequiresPolicy` | AuthorizationBehavior | `PolicyName` |

### 9.5 Resource-based authorization

Resource-based authorization (needing the loaded entity) is handled by `ResourceCommandHandler<TCommand,TEntity,TResponse>` template method, not a pipeline behavior. Sequence: `LoadAsync` → `Authorize` (returns `Failure?`) → `HandleAsync`.

## 10. Mediator

The `Mediator` class implements `IMediator` (which inherits `ISender` + `IPublisher`):

- **ISender** (`Send`): Dispatches queries, typed commands, and void commands through the pipeline. Uses cached `HandlerWrapper<TResponse>`/`VoidHandlerWrapper` instances created via `Activator.CreateInstance` on discovery.
- **IPublisher** (`Publish`): Dispatches notifications to all registered `INotificationHandler<T>` handlers. Supports `Sequential` and `Parallel` publish strategies.

The mediator resolves all handlers from DI - it has no internal handler registry (see ADR-0003).

## 11. DI registration

Both MS DI and Autofac support assembly-scanning `AddInterlocutor()` that registers:
- All `IQueryHandler<,>`, `ICommandHandler<,>`, `ICommandHandler<>`, `INotificationHandler<>` implementations as scoped
- `IMediator`, `ISender`, `IPublisher` as scoped

See `Interlocutor.DependencyInjection.Microsoft.ServiceCollectionExtensions` and `Interlocutor.DependencyInjection.Autofac.InterlocutorAutofacExtensions`.

### 11.1 Startup validation

`InterlocutorStartupValidation.ValidateHandlerRegistrations()` scans assemblies at startup and throws `InvalidOperationException` if any `IQuery<T>`, `ICommand<T>`, or `ICommand` type lacks a registered handler.

## 12. HTTP mapping

`ResultExtensions.ToActionResult()` maps each `Failure` case to HTTP:

| Failure case | HTTP Status | Body |
|-------------|-------------|------|
| `ValidationFailed` | 400 Bad Request | `ValidationProblemDetails` |
| `RuleViolation` | 422 Unprocessable Entity | `ProblemDetails` |
| `NotFound` | 404 Not Found | Empty |
| `Forbidden` | 403 Forbidden | Empty (reason logged server-side) |
| `Conflict` | 409 Conflict | `ProblemDetails` |

See [bootstrap §9](../bootstrapreference/PRD-mediator-result-pipeline.md#9-api-boundary-mapping) for the full mapping implementation.

## 13. Notifications

- `INotification` is a marker interface for domain events.
- `INotificationHandler<TNotification>` handles one notification type.
- Multiple handlers can register for the same notification.
- `PublishStrategy.Sequential` (default): handlers run in order, one by one.
- `PublishStrategy.Parallel`: handlers run concurrently via `Task.WhenAll`.

## 14. Ambient context

`IRequestContextAccessor` provides scoped ambient context with `CorrelationId`, `UserId`, `TenantId`, and `Items`. The `RequestContext` is an immutable record; the reference can be swapped via `Context { get; set; }`.

## 15. Comparison with bootstrap PRD

The bootstrap PRD was written before several features were implemented. Below is the reconciliation:

| Bootstrap gap | Implemented? | Location |
|--------------|--------------|----------|
| Void command pipeline (#08) | ✅ | `VoidCommandHandlerWrapper<TCommand>` in `Mediator.cs` |
| LoggingBehavior (#09) | ✅ | `LoggingBehavior` in `Behaviors.cs` |
| AuthorizationBehavior (#10) | ✅ | `AuthorizationBehavior` in `Behaviors.cs` |
| AuditingBehavior (#11) | ✅ | `AuditingBehavior` in `Behaviors.cs` |
| INotification/Publish (#13) | ✅ | `Mediator.Publish()` in `Mediator.cs` |
| ISender/IPublisher segregation (#14) | ✅ | Separate interfaces in `Abstractions.cs` |
| IRequestContextAccessor (#15) | ✅ | `RequestContextAccessor` + interface |
| Package version pinning (#01) | ✅ | `Directory.Packages.props` with CPM |
| README (#02) | ✅ | `README.md` |
| PascalCase naming (#06) | ✅ | `OperationOutcome<T>`, `ValidationErrors`, and the rest of the result-type vocabulary |

All features listed as gaps in bootstrap §12 are now implemented.

## 16. Testing strategy

- Handlers: unit-testable directly (bypass pipeline) for business logic.
- Pipeline: integration tests verify composition order, short-circuiting, and transaction lifecycle.
- DI: startup validation catches missing handler registrations at container build time.

## 17. Risks

- C# 15 union syntax is preview until .NET 11 GA (~November 2026). Usage isolated to `OperationOutcome.cs` in CommonKernel.
- Marker interface opt-in has no compiler enforcement. Recommended: architecture tests (NetArchTest present in `Directory.Packages.props`) asserting all `ICommand` implementers also implement `ITransactionalRequest` or `IExternalStoreRequest`.
- Struct-based unions box value-type `T`. Acceptable for mediator dispatch; flagged as optimization point.
