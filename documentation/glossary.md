# Glossary

Domain vocabulary used throughout the Interlocutor project. Use these terms consistently in code, tests, and documentation.

## Project

| Term | Definition |
|------|------------|
| **Interlocutor** | A mediator/CQRS pipeline library for .NET built on C# 15 union types. Named after the participant who conducts a dialogue - it mediates requests between code and handlers, which communicate via typed `Outcome<...>` unions (success, not found, forbidden, etc.). Namespace: `Interlocutor.*`. |

## Result Types

| Term | Definition |
|------|------------|
| **Outcome<T1, ...>** | C# 15 discriminated union, generic in 2 to 6 case types, named `T1..TN`: by convention the first type parameter is the success case and every remaining type parameter is an independent failure case (`NotFound`, `Forbidden`, `Conflict`, `Failure`, or a domain-specific type), though the union itself attaches no such meaning to any position. A handler is written against the specific arity and case types that operation can actually produce rather than routing every failure through one generic case. |
| **Outcome** | The case-less arity - a small, hand-written type (not generator-emitted, since it declares no case types), implementing `IUnion` directly with `Value` always `null`. Holding an instance is itself the acknowledgment that an operation ran; there is nothing to inspect on it. Reserved for a genuinely infallible operation that opts into none of `ICancellable`/`IHasTimeout`/`IRateLimited`. See [Messages, Result Types & Variance § ICommand (Unit Command)](messages-and-result-types.md#icommand-unit-command). |
| **Outcome<T>** | The single-case arity - a small, hand-written generic type (not generator-emitted, since there's only one case: nothing to discriminate), implementing `IUnion` directly. Exposes a strongly-typed `T Value` (not boxed) plus the same `Match`/`Switch`/`TryGetValue`/`ToString`/equality member shape every generic arity carries, so a caller never special-cases it - `TryGetValue` always succeeds, and `Match`/`Switch` take exactly one delegate. Reserved for an operation that always succeeds but still needs to carry exactly one value. See [Messages, Result Types & Variance § Outcome Unions](messages-and-result-types.md#outcome-unions). |
| **IUnion** | `System.Runtime.CompilerServices.IUnion` - the interface the compiler generates for every `union` declaration, exposing a single `object? Value` member. `IQuery<TResponse>` and `ICommand<TResponse>` both carry a `where TResponse : IUnion` constraint, so a bare DTO response type fails to compile - every query and command response is compiler-guaranteed to be a real union. This is what makes runtime-typed dispatch possible without the caller knowing the concrete outcome shape. Streaming (`IAsyncEnumerable<T>` or similar) is out of scope permanently - see [Messages, Result Types & Variance](messages-and-result-types.md) for the full reasoning. |
| **IOutcome** | Marker interface in `Interlocutor.CommonKernel.ResultTypes` for an opt-in classification surface, paired with `IOutcomeCase`'s `ActiveCaseType` - it is **not** the response-type constraint (that's `IUnion`, above). Every generic `Outcome<T1, ...>` arity still declares `: IOutcome` to expose that surface; neither the case-less, non-generic `Outcome` nor the single-case `Outcome<T>` does, since neither has more than the one case `ActiveCaseType` would need to distinguish. A hand-rolled response type may declare `IOutcome` too, but doing so is optional: `IUnion` alone is enough to be a legal `TResponse`. See [Messages, Result Types & Variance § IUnion as an Extension Point](messages-and-result-types.md#iunion-as-an-extension-point) for the full picture. |
| **GenerateOutcomeMembersAttribute** | Marks a `partial union` declaration for source-generated `Match`, `Switch`, per-case `TryGetValue`, `ToString`, and value equality (`Equals`, `GetHashCode`, `IEquatable<TSelf>`, `==`/`!=`) members - what every shipped `Outcome`/`Outcome<...>` arity itself is built from. A consumer's own `partial union` can carry the same attribute and get the identical generated surface, including for a custom arity beyond the shipped 2-6 case range. See [Extensibility § Custom-arity outcome unions](extensibility.md#custom-arity-outcome-unions-generateoutcomemembersattribute). |
| **IValidationFailureCase** | Minimal marker interface (a single `Messages` member) any validation-shaped failure case can implement - `ValidationErrors` implements it directly, and `Interlocutor.FluentValidation.FluentValidationErrors` implements it too, preserving a `FluentValidation.Results.ValidationFailure`'s full fidelity rather than collapsing it to just its message. Lets a consumer such as `DefaultHttpOutcomeStatusMapper` recognize any validation-shaped failure case generically, without knowing about every concrete type that implements it. |
| **IFieldValidationFailureCase** | Marker interface extending `IValidationFailureCase` with a `FieldMessages` member, for a validation-shaped failure case that can attribute each message to the property/field it applies to. `ValidationErrors` does not implement it (its `ValidationError` entries carry no property name); `Interlocutor.FluentValidation.FluentValidationErrors` does, since `FluentValidation.Results.ValidationFailure.PropertyName` survives the pipeline unmodified. Lets a consumer build field-keyed output (an HTTP `ValidationProblemDetails` body, for example) for any implementer generically. See [Extensibility § Mapping failure cases to any output shape](extensibility.md#mapping-failure-cases-to-any-output-shape-ioutcomemappertresult) for the sibling `IOutcomeMapper<TResult>` extension point that consumes it. |
| **FieldValidationMessage** | The entry type behind `IFieldValidationFailureCase.FieldMessages`: a validation failure message paired with the property/field it applies to (`PropertyName`, nullable for a cross-field/whole-object rule) and the `Message` itself. |
| **IRateLimitedFailureCase / ITimeoutFailureCase / IForbiddenFailureCase** | The same minimal-marker-interface shape as `IValidationFailureCase`, one per swappable pipeline concern: `RateLimited`, `Timeout`, and `Forbidden` each implement their own. `DefaultHttpOutcomeStatusMapper` recognizes all three generically, so a consumer who implements `IRateLimiter`/`ITimeoutPolicy`/`IPolicyEvaluator` with a technology-specific case type (richer than the built-in struct's fields) maps correctly without a mapper override - even though only the single built-in implementation of each ships today. |
| **Failure** | A simple `readonly record struct` carrying an optional human-readable `Reason`. Not a closed union of cases - it is the generic fallback failure case for when none of the more specific ones (`NotFound`, `Forbidden`, `Conflict`, `ValidationError`) apply. Never represents an unexpected exception; exceptions signal defects, not expected outcomes. For a machine-readable error code, use `Failure<TErrorCode>` instead. |
| **Success** | Marker record struct for unit command success - carries no payload. |
| **Singular/plural/marker naming convention** | The pattern every `ResultTypes` type follows: a singular default case (`Conflict`, `NotFound`, `Failure`) for one occurrence of a failure; a plural aggregate (`Conflicts`, `Failures`, `ValidationErrors`) for a handler that can fail for several independent reasons at once; and an `I<Concept>FailureCase` marker interface for substitutability. See [Messages, Result Types & Variance § Result Types](messages-and-result-types.md#result-types) for the full callout. |

## Failure Case Types

Independent record-struct types, each usable as one case type in an `Outcome<...>` union - not sub-cases of a single closed `Failure` union.

| Term | Definition |
|------|------------|
| **ValidationError** | A single validation failure: a message, or (via `ValidationError<TErrorCode>`) a message paired with a generic error code. Aggregated via `ValidationErrors` / `ValidationErrors<TErrorCode>`. Produced by validation behaviors (e.g., `Interlocutor.FluentValidation`). |
| **NotFound** | Requested entity does not exist. Produced by `ResourceCommandHandler` (when `LoadAsync` returns null) or handler code. A generic `NotFound<TId>` variant carries the entity type and ID separately. |
| **Forbidden** | Authorization denied for a specific resource. Produced by `ResourceCommandHandler.Authorize()` or `AuthorizationBehavior`. |
| **Conflict** | State conflict: concurrency, uniqueness violation, or invalid transition. Carries a message and a machine-readable code. Produced by handler code. |
| **Failure** | The generic fallback case - see Result Types above. |
| **Cancelled** | Cooperative cancellation of the ambient request, converted from an `OperationCanceledException` rather than letting it propagate as an unexpected fault. Reason-only, matching the shape of `NotFound`/`Forbidden` - a `CancellationToken` carries no queryable reason of its own. Produced by `CancellationBehavior` for requests implementing `ICancellable`. |
| **Timeout** | A handler exceeded its declared time budget. Carries the `Budget` that was allotted and the `Elapsed` time actually observed, plus an optional message. Produced by `TimeoutBehavior` for requests implementing `IHasTimeout`. Can only report a timeout, not enforce cancellation of the abandoned work - see the pipeline-and-behaviors reference doc. |
| **RateLimited** | The caller was throttled by a rate limiter. Carries the `LimitKey` identifying which limiter/bucket was hit and an optional `RetryAfter` hint. Produced by `RateLimitedBehavior` for requests implementing `IRateLimited`. |

## Result Type Aggregates

| Term | Definition |
|------|------------|
| **ValidationErrors** | Immutable collection of one or more `ValidationError` instances. De-duplicates preserving first-occurrence order. Safe at default value (returns empty collection). Exposes unified accessor `.Items` (primary), with semantic projections `.Errors` and `.Messages`. |
| **ValidationErrors<TErrorCode>** | Immutable collection of one or more `ValidationError<TErrorCode>` instances with associated error codes. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projections `.Errors`, `.Messages`, and `.ErrorCodes`. |
| **Failures** | Immutable collection of one or more `Failure` instances. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projection `.Reasons`. For failures that carry error codes, use `Failures<TErrorCode>`. |
| **Failures<TErrorCode>** | Immutable collection of one or more `Failure<TErrorCode>` instances with associated error codes. De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projections `.Reasons` and `.ErrorCodes`. |
| **Conflicts** | Immutable collection of one or more `Conflict` instances (state conflicts, concurrency, uniqueness violations). De-duplicates preserving first-occurrence order. Safe at default value. Exposes unified accessor `.Items` (primary), with semantic projection `.Messages`. |

## CQRS

| Term | Definition |
|------|------------|
| **Query** | A read request implementing `IQuery<TResponse>`, where `TResponse : IUnion`. Always returns `Outcome<...>` - the compiler rejects a bare DTO response type. Never writes data. Never participates in transactions. |
| **Command** | A write request implementing `ICommand<TResponse>` (returns a value, `TResponse : IUnion`) or `ICommand` (a unit command - no domain data, only an outcome). May participate in transactions. |
| **Unit command** | A command whose handler produces no domain data payload, only an outcome - `ICommand` / `IVoidCommandHandler<TCommand, TResult>`. Unlike `ICommand<TResponse>`, the outcome type isn't declared on the command - the caller picks it per dispatch via `ISender.Send<TResult>(ICommand, ...)`'s type argument. "void" is a misnomer this project no longer uses in prose, since nothing about the return value is actually void. |
| **Handler** | A class implementing `IQueryHandler<TQuery, TResponse>`, `ICommandHandler<TCommand, TResponse>`, or `IVoidCommandHandler<TCommand, TResult>`. Contains the business logic for exactly one request type (or, for an open generic handler, one request family). |

## Pipeline

| Term | Definition |
|------|------------|
| **Pipeline behavior** | A class implementing `IPipelineBehavior<TRequest, TResponse>`. Wraps handler execution in an onion/Russian-doll model using a `next()` delegate. Cross-cutting concerns (validation, transactions, logging) are pipeline behaviors. Nothing runs unless explicitly registered. |
| **Mediator** | The `Mediator` class implementing `IMediator`. Resolves handlers from DI, chains the behaviors that apply to the request in registration order, and dispatches. |

## Marker Interfaces

| Term | Definition |
|------|------------|
| **ITransactionalRequest** | Narrows, rather than opts in - every write already gets a transaction automatically via begin-on-first-write. Forces `UnitOfWorkBehavior` to begin the transaction eagerly, at pipeline start, so an earlier read shares it with a write that follows. |
| **IExternalStoreRequest** | Declares a command is backed by a non-SQL store/API. `UnitOfWorkBehavior` never opens a local transaction for it, even if it touches the scoped session. |
| **IAuditableRequest** | Opts a request into auditing. Carries `ActionName` used by `AuditingBehavior`. |
| **IRequiresPolicy** | Opts a request into non-resource authorization. Carries `PolicyName` evaluated by `AuthorizationBehavior`. |
| **ICancellable** | Opts a request into cooperative-cancellation reporting. No members - every request already carries its own `CancellationToken` through the pipeline. `CancellationBehavior` converts a cancellation of that token into `Cancelled` for requests implementing this marker. |
| **IHasTimeout** | Opts a request into time-budget enforcement. Carries `Budget`, the request's own declared time budget (subject to override by a registered `ITimeoutPolicy`). `TimeoutBehavior` races the rest of the pipeline against the resolved budget for requests implementing this marker. |
| **IRateLimited** | Opts a request into rate limiting. Carries `LimitKey`, identifying which limiter/bucket applies. `RateLimitedBehavior` checks the limit via a registered `IRateLimiter` for requests implementing this marker. |

## Notifications

| Term | Definition |
|------|------------|
| **INotification** | Marker interface for domain events/notifications. Has no return value - fire-and-forget. |
| **INotificationHandler<T>** | Handler for a notification type. Contravariant fan-out: a handler registered against a base event type or interface receives every derived notification. |
| **PublishStrategy** | Enum: `Sequential` (default - handlers run one by one, stop and propagate on the first exception, leaving partial execution reachable) or `Parallel` (every handler runs regardless of others failing, exceptions aggregated). Passed explicitly per call to `IMediator.Publish`; there is no attribute or configuration to declare relative ordering between handlers. |

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
| **RequestContext** | Immutable record with respect to its own properties - swap `CorrelationId`/`UserId`/`TenantId` via `Context { get; set; }`, not a setter. Null-safe - if unset, `Context` is null. `Items` is the one property that's settable in place - see `RequestContextItems`. |
| **RequestContextItems** | The mutable, thread-safe key-value bag behind `RequestContext.Items`. A handler or any pipeline behavior writes into it in place (`Set`/`TryGet`) at any point during a dispatch, with no record replacement needed - open-ended by design so a future ambient concern never requires a breaking change to `RequestContext` itself. |

## Pipeline Behaviors (built-in)

| Term | Definition |
|------|------------|
| **LoggingBehavior** | Logs request type, handler, duration, and outcome for every request that opts it in. Uses `ILogger<T>`. |
| **AuthorizationBehavior** | Enforces declarative authorization via `IPolicyEvaluator`. Short-circuits with `Forbidden` when policy fails. |
| **AuditingBehavior** | Records audit trail via `IAuditSink` for requests implementing `IAuditableRequest`. |
| **CancellationBehavior** | Converts cooperative cancellation of the ambient request into `Cancelled` for requests implementing `ICancellable`, instead of letting `OperationCanceledException` propagate as an unexpected fault. Opt-in, with no prescribed position in the pipeline. |
| **TimeoutBehavior** | Races the rest of the pipeline against a time budget resolved via `ITimeoutPolicy`, short-circuiting with `Timeout` if the budget elapses first, for requests implementing `IHasTimeout`. Can only report a timeout, not cancel the abandoned work - the pipeline has no mechanism to hand `next()` a shorter-lived token. Opt-in, with no prescribed position in the pipeline. |
| **RateLimitedBehavior** | Checks a rate limit via `IRateLimiter`, short-circuiting with `RateLimited` if it's exceeded, for requests implementing `IRateLimited`. Opt-in, with no prescribed position in the pipeline. |
| **PipelineStageBehavior** | Single host behavior aggregating all registered `IPipelineStage<TRequest, TResponse>` instances into the behavior chain - it is an ordinary entry in the list, never a second pipeline. |
| **IPipelineStage<TRequest, TResponse>** | Lightweight pre/post hook interface. Consumers implement `OnBeforeAsync` / `OnAfterAsync` without creating a full `IPipelineBehavior`. `OnBeforeAsync` returns `StageResult<TResponse>`: a stage may short-circuit and decline the request without the handler running. `TRequest` is contravariant. Both hooks default to a no-op via the `PipelineStageBase<TRequest, TResponse>` base class; stages can also be registered as delegates via `InterlocutorPipelineBuilder.AddStage` (reachable via `InterlocutorBuilder.Pipeline`). |
| **Eject** | Static helper (`Eject.With`) that forces a pipeline dispatch to end immediately with a developer-chosen outcome, from anywhere in a handler's or behavior's own call graph - not a stage, not tied to any marker interface, and unrelated to `CancellationBehavior`/`Cancelled`: cancellation reacts to the caller stopping the operation; `Eject.With` is a developer's own code deciding to stop it. See [Force-Ejecting From Deep In a Handler or Behavior](pipeline-and-behaviors.md#force-ejecting-from-deep-in-a-handler-or-behavior). |
| **PipelineEjectException<TResponse>** | Internal control-flow signal thrown by `Eject.With` and caught immediately at the nearest enclosing `Handle` call, converting it back into an ordinary response before anything further out in the chain can observe it. Never constructed directly, and never expected to cross back out of a mediator dispatch. |
| **NoCommit** | Static helper (`NoCommit.With`/`NoCommit.AndEject`) pairing a specific returned or ejected response value with an explicit "don't commit" signal, fully decoupled from that value's own case type - a call-site alternative to `IUnitOfWork.Cancel`'s ambient signal, for code that would rather not thread an injected unit of work through several calls just to force a rollback. See [Forcing a Rollback](transactions-and-store-integration.md#forcing-a-rollback). |

None of these run by default - the pipeline is empty until a builder call adds a behavior. The library documents a recommended chain (`UseRecommendedChain()`, five behaviors: Logging → Auditing → Authorization → Validation → Transaction) as opt-in sugar with its rationale, never as a default. `CancellationBehavior`, `TimeoutBehavior`, and `RateLimitedBehavior` are ordinary opt-in behaviors outside that chain, with no prescribed position - a consumer composes them wherever suits their application. See the pipeline-and-behaviors reference doc for the full ordering and reasoning.

## Outcome Case Registry

| Term | Definition |
|------|------------|
| **IOutcomeCaseRegistry** | Constructs an `IUnion` response from one of its case values, and extracts the active case type/value back out of an existing response, without either side knowing whether the response is a compiler-generated union or a hand-rolled `IUnion` implementation. `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, and the FluentValidation `ValidationBehavior` all depend on it to build their short-circuit response generically. `DefaultOutcomeCaseRegistry` is the zero-config default: it extracts any `IUnion` implementer's active case generically (reading `Value`), and constructs one via reflection when it's a compiler-generated union; `OutcomeCaseRegistryBuilder` lets a consumer register a custom factory for their own non-union `IUnion` implementation's construction, falling back to the default for anything unregistered. See [messages-and-result-types.md](messages-and-result-types.md) for the extension-point framing. |
| **ITimeoutPolicy** | Resolves the effective time budget for a request implementing `IHasTimeout`, letting a consumer override the request's own declared budget (e.g., a tenant-wide ceiling). `TimeoutBehavior` invokes it to resolve the budget it races against. `DefaultTimeoutPolicy` returns the request's own `Budget` unchanged. |
| **IRateLimiter** | Minimal rate-limiting abstraction with no framework coupling. `RateLimitedBehavior` invokes it to check whether a request implementing `IRateLimited` is allowed to proceed. An implementation can delegate to `System.Threading.RateLimiting`, a distributed limiter, or any throttling system. |

## Outcome Mapping

| Term | Definition |
|------|------------|
| **IOutcomeMapper\<TResult\>** | Transport-agnostic contract in `Interlocutor.Abstractions.Pipeline.Mapping`: maps a failure case value to a `TResult` representing it in some output format - an HTTP status and body, a gRPC status, a process exit code, anything a consumer needs to derive from a failure case. `Interlocutor.AspNetCore.IOutcomeStatusMapper` is its HTTP specialization (`TResult` fixed to `OutcomeHttpMapping`). See [Extensibility § Mapping failure cases to any output shape](extensibility.md#mapping-failure-cases-to-any-output-shape-ioutcomemappertresult). |
| **OutcomeMapperBase\<TResult\>** | Abstract base in `Interlocutor.Abstractions.Pipeline.Mapping` implementing `IOutcomeMapper<TResult>`'s dispatch over the built-in failure cases (`NotFound`, `Forbidden`, `Conflict`, `ValidationErrors`/`IValidationFailureCase`, `Failure`/`Failures`, `Cancelled`, `Timeout`, `RateLimited`, the generic `NotFound<TId>`), routing each to its own protected abstract method rather than requiring a concrete mapper to write the dispatch itself. `Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper` is the built-in `TResult = OutcomeHttpMapping` instantiation. |
| **IOutcomeStatusMapper** | The HTTP specialization of `IOutcomeMapper<TResult>` (`TResult` fixed to `OutcomeHttpMapping`), kept as its own name in `Interlocutor.AspNetCore` for discoverability. Maps a failure case to the status code and optional body `ToActionResult()`/`ToMinimalApiResult()` render. See [ASP.NET Core Integration § Custom HTTP Mapping](aspnetcore-integration.md#custom-http-mapping). |
| **DefaultHttpOutcomeStatusMapper** | The `IOutcomeStatusMapper` applied when a caller supplies none - built on `OutcomeMapperBase<OutcomeHttpMapping>`, overriding one method per failure case with this library's built-in status-code choices. See [ASP.NET Core Integration § Status Code Mapping](aspnetcore-integration.md#status-code-mapping). |

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
| **Transaction scope** | One logical request = one DI scope = one scoped `DbContext` / `ISession`, available for the whole pipeline regardless of transaction state. `UnitOfWorkBehavior` and `ResourceCommandHandler` receive the same scoped session via constructor injection. The mediator never opens its own scope - it borrows the ambient one. `IUnitOfWork` owns that session's transaction: it opens lazily, the first time anything writes (or eagerly, at pipeline start, for a request implementing `ITransactionalRequest`), and is committed or rolled back by `UnitOfWorkBehavior` at pipeline completion. |
| **Lazy resolution** | `UnitOfWorkBehavior`, `IUnitOfWork`, and `IDomainRepository<T>` all use `Lazy<TContext>`/`Lazy<ISession>` to defer session construction until something actually touches storage. A dispatch that never writes never opens a transaction; one that never touches storage at all never resolves the session. |

## DI Registration

| Term | Definition |
|------|------------|
| **InterlocutorBuilder** | The single container-neutral, fluent configuration entry point passed to `AddInterlocutor`. Owns assembly scanning, explicit handler/type-match registration, and the one ambient infrastructure default (`IRequestContextAccessor`); pipeline composition itself lives on `InterlocutorPipelineBuilder`, reachable via its `Pipeline` property. Accumulates registrations (handlers, behaviors, stages, infrastructure defaults) as `InterlocutorRegistration`/`InterlocutorFactoryRegistration` records; each container-specific package translates the accumulated list into its own registrations. Every mutating method returns the builder (or pipeline builder) itself for chaining. |
| **InterlocutorPipelineBuilder** | `InterlocutorBuilder.Pipeline`'s type - the sole entry point for pipeline composition: every `Add*Behavior` method, `AddBehavior`, `UseRecommendedChain`, the `AddStage`/`AddStageHost` family, the behavior-dependency `Override*` methods (`OverrideAuditSink`, `OverridePolicyEvaluator`, `OverrideOutcomeDescriber`, `OverrideTimeoutPolicy`, `OverrideRateLimiter`, `OverrideOutcomeCaseRegistry`), and `OverridePipelineFor` for declaring a per-request-type (or predicate-matched) pipeline that fully replaces the general-purpose one for matched requests. Can also be constructed standalone for testing pipeline composition in isolation from discovery concerns. |
| **RegistrationLifetime** | Container-neutral lifetime enum (`Singleton`, `Scoped`, `Transient`) used by every builder registration method in place of a container-specific lifetime type. |
| **RegistrationKind** | Classifies an accumulated registration as `Handler`, `Behavior`, `Stage`, `Dispatcher` (the generator-emitted `IGeneratedRequestDispatcher`, added alongside `Handler` entries when `AddGeneratedRegistrations` is used), or `Default`. Only `Default`-kind registrations use try-add precedence; the rest are always-add - duplicates are legal. |
| **RequireUniqueClosure** | Opts a consumer-supplied open-generic contract into the same "at most one implementation per closed instantiation" check the three built-in handler contracts already carry, so `ValidateOnBuild` also reports a conflict for it. |
| **Eject** | Removes one or more implementation types from consideration entirely - the escape hatch for clearing an ambiguous-closure conflict by dropping the losing candidate(s), for both built-in handler contracts and anything added via `RequireUniqueClosure`. |

## Pagination

These are not shipped types. `Interlocutor.CommonKernel` has no opinion about pagination - these rows illustrate one way a consumer composes their own paged response as a union payload.

| Term | Definition |
|------|------------|
| **PagedResult<T>** | A consumer-defined materialized page of results with paging metadata (`Items`, `Page`, `PageSize`, `TotalCount`), used as the success case type in a consumer's own `Outcome<PagedResult<T>, ...>`. Interlocutor never returns `IQueryable` and has no streaming response. |
| **IPagedQuery<TItem>** | A consumer-defined query contract carrying `Page` and `PageSize`, illustrating one shape a query returning `PagedResult<TItem>` might take. |

> [!NOTE]
> The optional `Interlocutor.Persistence.Abstractions` package ships its own `Page<T>` type - see [Persistence Abstraction](persistence-abstraction.md). That is a different package's vocabulary for a different concern: a repository's return shape, not an `Outcome` success-case shape. It does not contradict the above - `Interlocutor.CommonKernel` still ships no `PagedResult<T>` of its own.

## Packages

| Term | Definition |
|------|------------|
| **Interlocutor.CommonKernel** | Pure data types: `Outcome<...>` and its failure case types (`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError` / `ValidationErrors`). Zero external dependencies. |
| **Interlocutor.Abstractions** | Mediator interfaces, handler contracts, marker interfaces, `Mediator` implementation, pipeline behaviors (Logging, Authorization, Auditing, PipelineStage), and the transport-agnostic `IOutcomeMapper<TResult>`/`OutcomeMapperBase<TResult>` outcome-mapping abstraction. Depends on CommonKernel and framework-provided DI/Logging abstractions. |
| **Interlocutor.AspNetCore** | `ToActionResult()`-style HTTP mapping, delegating to a pluggable `IOutcomeStatusMapper` (the HTTP specialization of `IOutcomeMapper<TResult>`). Depends on CommonKernel, Abstractions, and ASP.NET Core. The only package in the solution permitted to know about HTTP. |
| **Interlocutor.DependencyInjection.Microsoft** | The builder-based registration entry point for Microsoft.Extensions.DependencyInjection. |
| **Interlocutor.DependencyInjection.Autofac** | The builder-based registration entry point for Autofac. |
| **Interlocutor.DependencyInjection.Abstractions** | Container-neutral registration model shared by the two DI packages - `InterlocutorBuilder`, `InterlocutorPipelineBuilder`, `RegistrationReport`. Depends on Abstractions only. |
| **Interlocutor.FluentValidation** | `ValidationBehavior` for FluentValidation. |
| **Interlocutor.Persistence.Abstractions** | Optional, opt-in `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` CRUD/paging surface. Zero ORM dependency; depends on Abstractions only - see [Persistence Abstraction](persistence-abstraction.md). |
| **Interlocutor.Persistence.EntityFrameworkCore** | EF Core implementation of the persistence abstraction, plus `UnitOfWorkBehavior` for EF Core `DbContext`. |
| **Interlocutor.Persistence.NHibernate** | NHibernate implementation of the persistence abstraction, plus `UnitOfWorkBehavior` for NHibernate `ISession`. |
| **Interlocutor.Generator** | The Roslyn incremental source generator behind compile-time handler discovery. Opt-in: a consumer references it directly as a `PrivateAssets="all"` analyzer package - Abstractions carries no reference to it, so it is never pulled in transitively. Never ships in build output either way. |
