# Context

Glossary of domain vocabulary used throughout the Mediance project. Use these terms consistently in issues, tests, and documentation.

## Project

| Term | Definition |
|------|------------|
| **Mediance** | A mediator/CQRS pipeline library for .NET built on C# 15 union types. Named after the legal term for how a case is resolved — maps to `OperationOutcome<T>` outcomes (succeeded, not found, forbidden, etc.). Namespace: `Mediance.*`. |

## Result Types

| Term | Definition |
|------|------------|
| **OperationOutcome\&lt;T\&gt;** | C# 15 discriminated union: either success (`T`) or `Failure`. Used as the return type of all queries and value-returning commands. |
| **OperationOutcome** | Non-generic union: either `Success` or `Failure`. Used as the return type of void commands. |
| **Failure** | Closed union of 5 expected failure outcomes: `ValidationFailed`, `RuleViolation`, `NotFound`, `Forbidden`, `Conflict`. Never represents an unexpected exception. |
| **Success** | Marker record for void command success. Maps to HTTP 204 No Content. |

## Failure Cases

| Term | Definition |
|------|------------|
| **ValidationFailed** | Input validation failure. Contains a dictionary of field → errors. Produced only by `ValidationBehavior` (FluentValidation). Maps to HTTP 400 Bad Request with `ValidationProblemDetails`. |
| **RuleViolation** | Business rule/logic violation. Produced by handler code after entity lookup. Maps to HTTP 422 Unprocessable Entity with `ProblemDetails`. |
| **NotFound** | Requested entity does not exist. Produced by `ResourceCommandHandler` (when `LoadAsync` returns null) or handler code. Maps to HTTP 404 Not Found (empty body). |
| **Forbidden** | Authorization denied for a specific resource. Produced by `ResourceCommandHandler.Authorize()` or `AuthorizationBehavior`. Maps to HTTP 403 Forbidden (empty body — reason is logged server-side only). |
| **Conflict** | State conflict: concurrency, uniqueness violation, or invalid transition. Produced by handler code. Maps to HTTP 409 Conflict with `ProblemDetails`. |

## Result Type Aggregates

| Term | Definition |
|------|------------|
| **ValidationErrors** | Immutable collection of one or more `ValidationError` instances. De-duplicates preserving first-occurrence order. Safe at default value (returns empty collection). Exposes unified accessor `.Items` (primary), with semantic projections `.Errors` and `.Messages`. |
| **ValidationErrors\&lt;TErrorCode\&gt;** | Immutable collection of one or more `ValidationError<TErrorCode>` instances with associated error codes. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projections `.Errors`, `.Messages`, and `.ErrorCodes`. |
| **Failures** | Immutable collection of one or more `Failure` instances. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projection `.Values` (legacy name). |
| **Conflicts** | Immutable collection of one or more `Conflict` instances (state conflicts, concurrency, uniqueness violations). De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projection `.Messages`. |

## CQRS

| Term | Definition |
|------|------------|
| **Query** | A read request implementing `IQuery<T>`. Always returns `OperationOutcome<T>`. Never writes data. Never participates in transactions. |
| **Command** | A write request implementing `ICommand<TResponse>` (returns value) or `ICommand` (void). Returns `OperationOutcome`/`OperationOutcome<T>`. May participate in transactions. |
| **Handler** | A class implementing `IQueryHandler<TQuery, TResponse>`, `ICommandHandler<TCommand, TResponse>`, or `ICommandHandler<TCommand>`. Contains the business logic for exactly one request type. |

## Pipeline

| Term | Definition |
|------|------------|
| **Pipeline behavior** | A class implementing `IPipelineBehavior<TRequest, TResponse>`. Wraps handler execution in an onion/Russian-doll model using a `next()` delegate. Cross-cutting concerns (validation, transactions, logging) are pipeline behaviors. |
| **Mediator** | The `Mediator` class implementing `IMediator`. Resolves handlers from DI, chains pipeline behaviors in reverse registration order, and dispatches requests. |

## Marker Interfaces

| Term | Definition |
|------|------------|
| **ITransactionalRequest** | Opts a command into the SQL unit-of-work. `TransactionBehavior` begins a transaction, commits on success, rolls back on Failure. |
| **IExternalStoreRequest** | Declares a command is backed by a non-SQL store/API. `TransactionBehavior` never resolves the SQL session for it. |
| **IAuditableRequest** | Opts a request into auditing. Carries `ActionName` used by `AuditingBehavior`. |
| **IRequiresPolicy** | Opts a request into non-resource authorization. Carries `PolicyName` evaluated by `AuthorizationBehavior`. |

## Notifications

| Term | Definition |
|------|------------|
| **INotification** | Marker interface for domain events/notifications. Has no return value — fire-and-forget. |
| **INotificationHandler\<T\>** | Handler for a notification type. Multiple handlers may register for the same notification. Return type is `Task` (no result). |
| **PublishStrategy** | Enum: `Sequential` (handlers run in order, stop on failure) or `Parallel` (handlers run concurrently, aggregate exceptions). |

## Interface Segregation

| Term | Definition |
|------|------------|
| **ISender** | Narrow interface for send-only (request/response). Exposes `Send()` for queries and commands. |
| **IPublisher** | Narrow interface for publish-only (fire-and-forget). Exposes `Publish()` for notifications. |
| **IMediator** | Combined convenience interface inheriting both `ISender` and `IPublisher`. Used by root-level code (controllers, background jobs). |

## Ambient Context

| Term | Definition |
|------|------------|
| **IRequestContextAccessor** | Scoped ambient context carrying `CorrelationId`, `UserId`, `TenantId`, and `Items` through the request pipeline. |
| **RequestContext** | Immutable record (readonly, swap via `Context { get; set; }`). Null-safe — if unset, `Context` is null. |

## Pipeline Behaviors (built-in)

| Term | Definition |
|------|------------|
| **LoggingBehavior** | Logs request type, handler, duration, and outcome for every mediator request. Uses `ILogger<T>`. |
| **AuthorizationBehavior** | Enforces declarative authorization via `IPolicyEvaluator`. Short-circuits with `Forbidden` when policy fails. |
| **AuditingBehavior** | Records audit trail via `IAuditSink` for requests implementing `IAuditableRequest`. |
| **PipelineStageBehavior** | Generic ingress/egress hook aggregating all registered `IPipelineStage<TRequest,TResponse>` instances. |
| **IPipelineStage\<TRequest,TResponse\>** | Lightweight pre/post hook interface. Consumers implement `OnBeforeAsync`/`OnAfterAsync` without creating a full `IPipelineBehavior`. |

## Auditing

| Term | Definition |
|------|------------|
| **IAuditSink** | Minimal interface for writing audit records. Single method: `RecordAsync(AuditEntry, CancellationToken)`. |
| **AuditEntry** | Record containing `ActionName`, `RequestType`, `Outcome`, `Timestamp`, and optional `UserId`. |

## Authorization

| Term | Definition |
|------|------------|
| **IPolicyEvaluator** | Minimal interface (no ASP.NET Core dependency) for evaluating authorization policies. Single method: `EvaluateAsync(policyName, request, ct)`. Bridges to ASP.NET Core's `IAuthorizationService` in consumer code via a simple adapter. |

## Pagination

| Term | Definition |
|------|------------|
| **PagedResult\&lt;T\&gt;** | Materialized page of results with paging metadata (`Items`, `Page`, `PageSize`, `TotalCount`). Used as `T` in `OperationOutcome<PagedResult<T>>`. Always materialized — never returns `IQueryable`. |
| **IPagedQuery\&lt;TItem\&gt;** | A query that returns `PagedResult<TItem>`. Carries `Page` and `PageSize` as contract members. |

## Resource Authorization

| Term | Definition |
|------|------------|
| **ResourceCommandHandler** | Abstract base class implementing the load → authorize → handle template method pattern. Used when authorization depends on the loaded entity (ownership, claims against resource state). Not a pipeline behavior — the pattern is built into the handler. |

## Transaction Model

| Term | Definition |
|------|------------|
| **Transaction scope** | One logical request = one DI scope = one scoped `DbContext`/`ISession`. `TransactionBehavior` and `ResourceCommandHandler` receive the same scoped session via constructor injection. |
| **Lazy resolution** | `TransactionBehavior` uses `Lazy<TContext>` to defer session construction until it's confirmed the request implements `ITransactionalRequest`. Non-transactional requests never resolve the session. |

## Packages

| Term | Definition |
|------|------------|
| **Mediance.CommonKernel** | Pure data types (OperationOutcome, Failure, PagedResult). Zero external dependencies. |
| **Mediance.Abstractions** | Mediator interfaces, handler contracts, marker interfaces, Mediator implementation, pipeline behaviors (Logging, Authorization, Auditing, PipelineStage). Depends on CommonKernel and framework-provided DI/Logging abstractions. |
| **Mediance.AspNetCore** | `ToActionResult()` HTTP mapping. Depends on CommonKernel and ASP.NET Core. |
| **Mediance.DependencyInjection.Microsoft** | Assembly-scanning `AddMediance()` for Microsoft.Extensions.DependencyInjection. |
| **Mediance.DependencyInjection.Autofac** | Assembly-scanning `AddMediance()` for Autofac. |
| **Mediance.EntityFrameworkCore** | `TransactionBehavior` for EF Core `DbContext`. |
| **Mediance.NHibernate** | `TransactionBehavior` for NHibernate `ISession`. |
| **Mediance.FluentValidation** | `ValidationBehavior` and `BulkItemValidationBehavior` for FluentValidation. |
