# Glossary

Domain vocabulary used throughout the Interlocutor project. Use these terms consistently in code, tests, and documentation.

## Project

| Term | Definition |
|------|------------|
| **Interlocutor** | A mediator/CQRS pipeline library for .NET built on C# 15 union types. Named after the participant who conducts a dialogue - it mediates requests between code and handlers, which communicate via typed `OperationOutcome<...>` unions (success, not found, forbidden, etc.). Namespace: `Interlocutor.*`. |

## Result Types

| Term | Definition |
|------|------------|
| **OperationOutcome<TSuccess, ...>** | C# 15 discriminated union, generic in 2 to 6 case types: the first type parameter is the success case, every remaining type parameter is an independent failure case (`NotFound`, `Forbidden`, `Conflict`, `Failure`, or a domain-specific type). A handler is written against the specific arity and case types that operation can actually produce rather than routing every failure through one generic case. |
| **OperationOutcome** | Non-generic union: either `Success` or `Failure`. Used as the return type of unit commands. |
| **IOperationOutcome** | Marker interface every `OperationOutcome`/`OperationOutcome<...>` union implements. `IQuery<TResponse>` and `ICommand<TResponse>` both carry a `where TResponse : IOperationOutcome` constraint, so a bare DTO response type fails to compile - every query and command response is compiler-guaranteed to be an outcome. This is what makes runtime-typed dispatch possible without the caller knowing the concrete outcome shape. Streaming (`IAsyncEnumerable<T>` or similar) is out of scope permanently - see [Messages, Result Types & Variance](messages-and-result-types.md) for the full reasoning. |
| **Failure** | A simple `readonly record struct` carrying an optional human-readable `Reason`, an optional machine-readable `ErrorCode`, or both. Not a closed union of cases - it is the generic fallback failure case for when none of the more specific ones (`NotFound`, `Forbidden`, `Conflict`, `ValidationError`) apply. Never represents an unexpected exception; exceptions signal defects, not expected outcomes. |
| **Success** | Marker record struct for unit command success - carries no payload. |

## Failure Case Types

Independent record-struct types, each usable as one case type in an `OperationOutcome<...>` union - not sub-cases of a single closed `Failure` union.

| Term | Definition |
|------|------------|
| **ValidationError** | A single validation failure: a message, or a message paired with a generic error code. Aggregated via `ValidationErrors` / `ValidationErrors<TErrorCode>`. Produced by validation behaviors (e.g., `Interlocutor.FluentValidation`). |
| **NotFound** | Requested entity does not exist. Produced by `ResourceCommandHandler` (when `LoadAsync` returns null) or handler code. A generic `NotFound<TId>` variant carries the entity type and ID separately. |
| **Forbidden** | Authorization denied for a specific resource. Produced by `ResourceCommandHandler.Authorize()` or `AuthorizationBehavior`. |
| **Conflict** | State conflict: concurrency, uniqueness violation, or invalid transition. Carries a message and a machine-readable code. Produced by handler code. |
| **Failure** | The generic fallback case - see Result Types above. |

## Result Type Aggregates

| Term | Definition |
|------|------------|
| **ValidationErrors** | Immutable collection of one or more `ValidationError` instances. De-duplicates preserving first-occurrence order. Safe at default value (returns empty collection). Exposes unified accessor `.Items` (primary), with semantic projections `.Errors` and `.Messages`. |
| **ValidationErrors<TErrorCode>** | Immutable collection of one or more `ValidationError<TErrorCode>` instances with associated error codes. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projections `.Errors`, `.Messages`, and `.ErrorCodes`. |
| **Failures** | Immutable collection of one or more `Failure` instances. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary). |
| **Conflicts** | Immutable collection of one or more `Conflict` instances (state conflicts, concurrency, uniqueness violations). De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projection `.Messages`. |

## CQRS

| Term | Definition |
|------|------------|
| **Query** | A read request implementing `IQuery<TResponse>`, where `TResponse : IOperationOutcome`. Always returns `OperationOutcome<...>` - the compiler rejects a bare DTO response type. Never writes data. Never participates in transactions. |
| **Command** | A write request implementing `ICommand<TResponse>` (returns a value, `TResponse : IOperationOutcome`) or `ICommand` (a unit command - no domain data, only an outcome). Returns `OperationOutcome` / `OperationOutcome<...>`. May participate in transactions. |
| **Unit command** | A command whose handler produces no domain data payload, only success or failure - `ICommand` / `ICommandHandler<TCommand>`. Always returns an outcome; "void" is a misnomer this project no longer uses in prose, since nothing about the return value is actually void. |
| **Handler** | A class implementing `IQueryHandler<TQuery, TResponse>`, `ICommandHandler<TCommand, TResponse>`, or `ICommandHandler<TCommand>`. Contains the business logic for exactly one request type (or, for an open generic handler, one request family). |

## Pipeline

| Term | Definition |
|------|------------|
| **Pipeline behavior** | A class implementing `IPipelineBehavior<TRequest, TResponse>`. Wraps handler execution in an onion/Russian-doll model using a `next()` delegate. Cross-cutting concerns (validation, transactions, logging) are pipeline behaviors. Nothing runs unless explicitly registered. |
| **Mediator** | The `Mediator` class implementing `IMediator`. Resolves handlers from DI, chains the behaviors that apply to the request in registration order, and dispatches. |

## Marker Interfaces

| Term | Definition |
|------|------------|
| **ITransactionalRequest** | Opts a command into the SQL unit-of-work. `TransactionBehavior` begins a transaction, commits on success, rolls back on failure. |
| **IExternalStoreRequest** | Declares a command is backed by a non-SQL store/API. `TransactionBehavior` never resolves the SQL session for it. |
| **IAuditableRequest** | Opts a request into auditing. Carries `ActionName` used by `AuditingBehavior`. |
| **IRequiresPolicy** | Opts a request into non-resource authorization. Carries `PolicyName` evaluated by `AuthorizationBehavior`. |

## Notifications

| Term | Definition |
|------|------------|
| **INotification** | Marker interface for domain events/notifications. Has no return value - fire-and-forget. |
| **INotificationHandler<T>** | Handler for a notification type. Contravariant fan-out: a handler registered against a base event type or interface receives every derived notification. |
| **PublishStrategy** | Enum: `Sequential` (default - handlers run one by one, stop and propagate on the first exception, leaving partial execution reachable) or `Parallel` (every handler runs regardless of others failing, exceptions aggregated). Handler ordering is unspecified in general; a relative run-after/run-before attribute is the escape hatch for a genuine ordering need. |

## Interface Segregation

| Term | Definition |
|------|------------|
| **ISender** | Narrow interface for send-only (request/response). Exposes `Send()` for queries and commands. |
| **IPublisher** | Narrow interface for publish-only (fire-and-forget). Exposes `Publish()` for notifications. |
| **IMediator** | Combined convenience interface inheriting both `ISender` and `IPublisher`. Used by root-level code (controllers, background jobs). Registered scoped and non-configurable; never create your own scope from it - see [Mediator, Lifetimes, Scope & Ambient Context](mediator-lifetimes-and-scope.md). |

## Ambient Context

| Term | Definition |
|------|------------|
| **IRequestContextAccessor** | Scoped ambient context carrying `CorrelationId`, `UserId`, `TenantId`, and `Items` through the request pipeline. |
| **RequestContext** | Immutable record (readonly, swap via `Context { get; set; }`). Null-safe - if unset, `Context` is null. |

## Pipeline Behaviors (built-in)

| Term | Definition |
|------|------------|
| **LoggingBehavior** | Logs request type, handler, duration, and outcome for every request that opts it in. Uses `ILogger<T>`. |
| **AuthorizationBehavior** | Enforces declarative authorization via `IPolicyEvaluator`. Short-circuits with `Forbidden` when policy fails. |
| **AuditingBehavior** | Records audit trail via `IAuditSink` for requests implementing `IAuditableRequest`. |
| **PipelineStageBehavior** | Single host behavior aggregating all registered `IPipelineStage<TRequest, TResponse>` instances into the behavior chain - it is an ordinary entry in the list, never a second pipeline. |
| **IPipelineStage<TRequest, TResponse>** | Lightweight pre/post hook interface. Consumers implement `OnBeforeAsync` / `OnAfterAsync` without creating a full `IPipelineBehavior`. `OnBeforeAsync` returns `StageResult<TResponse>`: a stage may short-circuit and decline the request without the handler running. `TRequest` is contravariant. Both hooks default to a no-op via the `PipelineStageBase<TRequest, TResponse>` base class; stages can also be registered as delegates via `InterlocutorBuilder.AddStage`. |

None of these run by default - the pipeline is empty until a builder call adds a behavior. The library documents a recommended chain as opt-in sugar with its rationale, never as a default. See the pipeline-and-behaviors reference doc for the full ordering and reasoning.

## Auditing

| Term | Definition |
|------|------------|
| **IAuditSink** | Minimal interface for writing audit records. Single method: `RecordAsync(AuditEntry, CancellationToken)`. |
| **AuditEntry** | Record containing `ActionName`, `RequestType`, `Outcome`, `Timestamp`, and optional `UserId`. |

## Authorization

| Term | Definition |
|------|------------|
| **IPolicyEvaluator** | Minimal interface (no ASP.NET Core dependency) for evaluating authorization policies. Single method: `EvaluateAsync(policyName, request, ct)`. Bridged to ASP.NET Core's `IAuthorizationService` in consumer code via a simple adapter. |

## Resource Authorization

| Term | Definition |
|------|------------|
| **ResourceCommandHandler** | Abstract base class implementing the load → authorize → handle template method pattern. Used when authorization depends on the loaded entity (ownership, claims against resource state). Not a pipeline behavior - the pattern is built into the handler. |

## Transaction Model

| Term | Definition |
|------|------------|
| **Transaction scope** | One logical request = one DI scope = one scoped `DbContext` / `ISession`. `TransactionBehavior` and `ResourceCommandHandler` receive the same scoped session via constructor injection. The mediator never opens its own scope - it borrows the ambient one. |
| **Lazy resolution** | `TransactionBehavior` uses `Lazy<TContext>` to defer session construction until it's confirmed the request implements `ITransactionalRequest`. Non-transactional requests never resolve the session. |

## DI Registration

| Term | Definition |
|------|------------|
| **InterlocutorBuilder** | The single container-neutral, fluent configuration entry point passed to `AddInterlocutor`. Accumulates registrations (handlers, behaviors, stages, infrastructure defaults) as `InterlocutorRegistration`/`InterlocutorFactoryRegistration` records; each container-specific package translates the accumulated list into its own registrations. Every mutating method returns the builder itself for chaining. |
| **RegistrationLifetime** | Container-neutral lifetime enum (`Singleton`, `Scoped`, `Transient`) used by every builder registration method in place of a container-specific lifetime type. |
| **RegistrationKind** | Classifies an accumulated registration as `Handler`, `Behavior`, `Default`, or `Stage`. Only `Default`-kind registrations use try-add precedence; the rest are always-add - duplicates are legal. |

## Pagination

These are not shipped types. Interlocutor's core has no opinion about pagination - these rows illustrate one way a consumer composes their own paged response as a union payload.

| Term | Definition |
|------|------------|
| **PagedResult<T>** | A consumer-defined materialized page of results with paging metadata (`Items`, `Page`, `PageSize`, `TotalCount`), used as the success case type in a consumer's own `OperationOutcome<PagedResult<T>, ...>`. Interlocutor never returns `IQueryable` and has no streaming response. |
| **IPagedQuery<TItem>** | A consumer-defined query contract carrying `Page` and `PageSize`, illustrating one shape a query returning `PagedResult<TItem>` might take. |

## Packages

| Term | Definition |
|------|------------|
| **Interlocutor.CommonKernel** | Pure data types: `OperationOutcome<...>` and its failure case types (`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError` / `ValidationErrors`). Zero external dependencies. |
| **Interlocutor.Abstractions** | Mediator interfaces, handler contracts, marker interfaces, `Mediator` implementation, pipeline behaviors (Logging, Authorization, Auditing, PipelineStage). Depends on CommonKernel and framework-provided DI/Logging abstractions. |
| **Interlocutor.AspNetCore** | `ToActionResult()`-style HTTP mapping, delegating to a pluggable mapper. Depends on CommonKernel and ASP.NET Core. The only package in the solution permitted to know about HTTP. |
| **Interlocutor.DependencyInjection.Microsoft** | The builder-based registration entry point for Microsoft.Extensions.DependencyInjection. |
| **Interlocutor.DependencyInjection.Autofac** | The builder-based registration entry point for Autofac. |
| **Interlocutor.EntityFrameworkCore** | `TransactionBehavior` for EF Core `DbContext`. |
| **Interlocutor.NHibernate** | `TransactionBehavior` for NHibernate `ISession`. |
| **Interlocutor.FluentValidation** | `ValidationBehavior` and `BulkItemValidationBehavior` for FluentValidation. |
