# Interlocutor.Abstractions API Reference

Every public type in `Interlocutor.Abstractions`, grouped by namespace - the mediator core, [handlers](../glossary.md#cqrs), and [pipeline behaviors](../glossary.md#pipeline).

## Interlocutor.Abstractions

### `InterlocutorStartupValidation` class

Provides startup validation that all handler types are properly registered in the DI container.

| Member | Summary |
| --- | --- |
| `public static IServiceProvider ValidateHandlerRegistrations(IServiceProvider provider, params Assembly[] assembliesToScan)` | Validates that all handler types referenced by query and command types in the specified assemblies are registered in the DI container. |
| `public static IServiceProvider ValidatePipelineCaseFidelity(IServiceProvider provider, params Assembly[] assembliesToScan)` | Validates that every scanned request type's response is shaped to satisfy the pipeline behaviors its marker interfaces (and, for FluentValidation, its actually-registered validators) opt it into. |

### `Mediator` sealed class

Default mediator that dispatches queries, commands, and notifications to their registered handlers through pipeline behaviors. Handler wrapper instances are cached by request type to avoid repeated reflection.

| Member | Summary |
| --- | --- |
| `public Mediator(IServiceProvider services)` |  |
| `public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default(CancellationToken))` |  |
| `public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<IOutcome> Send(object request, CancellationToken ct = default(CancellationToken))` |  |

## Interlocutor.Abstractions.Commands

### `ICommand` interface

Marks a command that produces no response data.


### `ICommand<TResponse>` interface

Marks a command that produces a typed response of type `TResponse`.


### `ICommandHandler<TCommand, TResponse>` interface

Handles a command that returns a typed response.

| Member | Summary |
| --- | --- |
| `Task<TResponse> Handle(TCommand command, CancellationToken ct)` | Handles the specified command and returns a result containing the response or a failure. |

### `IBareCommandHandler<TCommand, TResult>` interface

Handles a command that produces no data payload - only an outcome.

| Member | Summary |
| --- | --- |
| `Task<TResult> Handle(TCommand command, CancellationToken ct)` | Handles the specified command and returns the outcome. |

### `ResourceCommandHandler<TCommand, TEntity, TSuccess>` abstract class

Provides a template method for command handlers that load, authorize, and then act on a domain entity.

| Member | Summary |
| --- | --- |
| `public Task<Outcome<TSuccess, NotFound, Forbidden>> Handle(TCommand command, CancellationToken ct)` | Executes the load-authorize-handle pipeline for the given command. |

## Interlocutor.Abstractions.Core

### `AmbientRequestContextAccessor` sealed class

Framework-agnostic ambient `IRequestContextAccessor` implementation with scoped lifetime.

| Member | Summary |
| --- | --- |
| `public AmbientRequestContextAccessor()` |  |
| `public RequestContext? Context` |  |

### `HandlerNotRegisteredException` sealed class

Thrown by runtime-typed dispatch (`Send`) when the request's own type is a recognized request shape (it closes IQuery<TResponse>, ICommand<TResponse>, or implements ICommand) but no handler for it is registered in the service provider.

| Member | Summary |
| --- | --- |
| `public HandlerNotRegisteredException(Type requestType, Type? handlerType = null)` | Initializes a new instance of the `HandlerNotRegisteredException` class. |
| `public Type? HandlerType` | Gets the specific closed handler interface that was expected to be registered, or `null` when no single closed handler interface could be determined. |
| `public Type RequestType` | Gets the runtime type of the request that has no registered handler. |

### `HandlerOverrideAttribute` sealed class

Declares that the annotated handler takes precedence over `handlerType` when both would otherwise match the same closed request type ambiguously.

| Member | Summary |
| --- | --- |
| `public HandlerOverrideAttribute(Type handlerType)` |  |
| `public Type HandlerType` | Gets the handler type the annotated handler overrides. |

### `IGeneratedNotificationHandlerOrder` interface

Implemented by Interlocutor.Generator's emitted notification handler order table - the compile-time computed relative order for notification handlers whose declaring type carries a NotificationHandlerRunAfterAttribute or NotificationHandlerRunBeforeAttribute.

| Member | Summary |
| --- | --- |
| `int Rank(Type handlerType)` | Returns `handlerType`'s position in the compile-time computed topological order, or `MaxValue` if it was never involved in a run-after/run-before relationship. |

### `IGeneratedRequestDispatcher` interface

Implemented by Interlocutor.Generator's emitted type switch - the reflection-free replacement for `Send`'s hot path when the generator had visibility into the full set of request types in the compilation.

| Member | Summary |
| --- | --- |
| `Task<IOutcome> Dispatch(object request, IServiceProvider services, CancellationToken ct)` | Dispatches `request` - whose runtime type is only known via a compile-time-generated type switch, not reflection - to its registered handler, returning the result as the library's base outcome interface. |

### `IMediator` interface

Combined send-and-publish interface. Exposes both `ISender` and `IPublisher`.


### `IPublisher` interface

Publish-only interface for fire-and-forget notifications. Exposes `Publish`.

| Member | Summary |
| --- | --- |
| `Task Publish<TNotification>(TNotification notification, CancellationToken ct = default(CancellationToken))` | Publishes a notification to every applicable `INotificationHandler` instance, using `Sequential`. |
| `Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default(CancellationToken))` | Publishes a notification to every applicable `INotificationHandler` instance, using `strategy`. |

### `IRequestContextAccessor` interface

Scoped ambient context carrying correlation ID, user identity, and tenant information through the request pipeline.

| Member | Summary |
| --- | --- |
| `RequestContext? Context` | Gets or sets the current request context. May be null if not populated. |

### `ISender` interface

Send-only interface for request/response operations. Exposes Send overloads for queries and commands.

| Member | Summary |
| --- | --- |
| `Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default(CancellationToken))` | Dispatches a query to its registered handler, returning the result through all pipeline behaviors. |
| `Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default(CancellationToken))` | Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors. |
| `Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default(CancellationToken))` | Dispatches a bare command to its registered handler, returning the outcome through all pipeline behaviors. |
| `Task<IOutcome> Send(object request, CancellationToken ct = default(CancellationToken))` | Dispatches a request whose static type is not known at the call site - a query, a value-returning command, or a bare command deserialized off a message - to its registered handler, returning the result through all pipeline behaviors as the library's base outcome interface. |

### `MediatorResolvedFromRootScopeException` sealed class

Thrown when `IMediator`/`ISender`/`IPublisher` dispatch (Send or Publish) is attempted on an instance resolved directly from the root service provider/container, rather than from a real scope (a per-request scope, or one explicitly created via IServiceScopeFactory.CreateScope()/Autofac's BeginLifetimeScope()).

| Member | Summary |
| --- | --- |
| `public MediatorResolvedFromRootScopeException(Type requestType)` | Initializes a new instance of the `MediatorResolvedFromRootScopeException` class. |
| `public Type RequestType` | Gets the runtime type of the request or notification being dispatched when the root-scope guard fired. |

### `PublishStrategy` enum

Controls how notifications are dispatched to multiple handlers.

| Member | Summary |
| --- | --- |
| `Parallel = 1` | Handlers execute concurrently via Task.WhenAll. Exceptions are aggregated. |
| `Sequential = 0` | Handlers execute in registration order. Stops on the first exception. |

### `RequestContext` record

Immutable snapshot of request-scoped ambient data.

| Member | Summary |
| --- | --- |
| `public RequestContext()` |  |
| `public string CorrelationId` | Gets a unique identifier for correlating this request across services. Auto-generated if not set. |
| `public RequestContextItems Items` | Gets a mutable, thread-safe key-value bag for extension data - always non-null. |
| `public string? TenantId` | Gets the tenant identifier for multi-tenant scenarios, or null if not scoped. |
| `public string? UserId` | Gets the authenticated user identifier, or null if anonymous. |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(RequestContext? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(RequestContext? left, RequestContext? right)` |  |
| `public static bool operator !=(RequestContext? left, RequestContext? right)` |  |

### `RequestContextItems` sealed class

A mutable, thread-safe, request-scoped key-value bag carried on `Items`.

| Member | Summary |
| --- | --- |
| `public RequestContextItems()` | Initializes a new instance of the `RequestContextItems` class, with an empty item bag. |
| `public RequestContextItems(IEnumerable<KeyValuePair<string, object?>> seed)` | Initializes a new instance of the `RequestContextItems` class, seeded with `seed`. |
| `public object? this[string key]` | Gets or sets the value stored under `key`, or `null` if not present. |
| `public void Clear()` | Removes every entry. Useful when a DI scope is reused across more than one dispatch (see this type's remarks on granularity) and a later dispatch should not see an earlier one's entries. |
| `public bool ContainsKey(string key)` | Determines whether `key` has a stored value. |
| `public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()` |  |
| `public TValue GetOrAdd<TValue>(string key, Func<string, TValue> factory)` | Gets the value stored under `key`, creating and storing one via `factory` if absent. |
| `public bool Remove(string key)` | Removes the entry stored under `key`, if present. |
| `public void Set<TValue>(string key, TValue value)` | Sets `key` to `value`, overwriting any existing value. |
| `public bool TryGet<TValue>(string key, out TValue value)` | Attempts to get the value stored under `key`, typed as `TValue`. |

### `UnsupportedRequestTypeException` sealed class

Thrown by runtime-typed dispatch (`Send`) when the given object does not implement any recognized request contract - it closes neither IQuery<TResponse> nor ICommand<TResponse>, and does not implement ICommand.

| Member | Summary |
| --- | --- |
| `public UnsupportedRequestTypeException(Type requestType)` | Initializes a new instance of the `UnsupportedRequestTypeException` class. |
| `public Type RequestType` | Gets the runtime type of the object that is not a recognized request shape. |

## Interlocutor.Abstractions.Diagnostics

### `MediatorActivitySource` class

Hosts the single `ActivitySource` the mediator uses to emit distributed-tracing spans for every dispatch, independent of whether `LoggingBehavior` is also registered.

| Member | Summary |
| --- | --- |
| `public const string HandlerTypeTag = "interlocutor.handler_type"` | The tag key carrying the runtime type(s) of the handler(s) resolved to serve the request. |
| `public const string Name = "Interlocutor.Mediator"` | The stable, documented name of the mediator's `ActivitySource`. Register this name with a listener (an `ActivityListener`, or an OpenTelemetry AddSource call) to observe mediator dispatch spans. |
| `public const string OutcomeTag = "interlocutor.outcome"` | The tag key carrying the human-readable label of the resulting Outcome case. |
| `public const string RequestTypeTag = "interlocutor.request_type"` | The tag key carrying the runtime type of the dispatched request or notification. |

## Interlocutor.Abstractions.Markers

### `IAuditableRequest` interface

Marks a request that should be audited upon execution.

| Member | Summary |
| --- | --- |
| `string ActionName` | Gets the logical name of the action recorded in the audit log. |

### `ICancellableRequest` interface

Marks a request whose cooperative cancellation should be converted into a `Cancelled` outcome instead of letting `OperationCanceledException` propagate as an unexpected fault.


### `IExternalStoreRequest` interface

Marks a request whose handler manages its own transaction scope against an external system, so the store-specific commit-gate behavior should not wrap it in a local one.


### `ITimeoutRequest` interface

Marks a request that declares a time budget it must complete within.

| Member | Summary |
| --- | --- |
| `TimeSpan Budget` | Gets the time budget this request should complete within. |

### `IRateLimitedRequest` interface

Marks a request that is subject to rate limiting before execution.

| Member | Summary |
| --- | --- |
| `string LimitKey` | Gets the key identifying which rate limiter/bucket applies to this request. |

### `IRequiresPolicyRequest` interface

Marks a request that must satisfy an authorization policy before execution.

| Member | Summary |
| --- | --- |
| `string PolicyName` | Gets the name of the authorization policy to enforce. |

### `ITransactionalRequest` interface

Marks a request whose read(s) and later write(s) must be protected by one eager transaction from pipeline start, rather than the default lazy, begin-on-first-write transaction every write already gets.


## Interlocutor.Abstractions.Notifications

### `INotification` interface

Marker interface for domain events or notifications. Has no return value - fire-and-forget.


### `INotificationHandler<TNotification>` interface

Handles a single notification type. Multiple handlers may register for the same `TNotification`.

| Member | Summary |
| --- | --- |
| `Task Handle(TNotification notification, CancellationToken ct)` | Processes the notification. |

### `NotificationHandlerRunAfterAttribute` sealed class

Declares that the annotated `INotificationHandler` implementation must run after `handlerType` whenever both are resolved for the same publish.

| Member | Summary |
| --- | --- |
| `public NotificationHandlerRunAfterAttribute(Type handlerType)` |  |
| `public Type HandlerType` | Gets the handler type the annotated handler must run after. |

### `NotificationHandlerRunBeforeAttribute` sealed class

Declares that the annotated `INotificationHandler` implementation must run before `handlerType` whenever both are resolved for the same publish.

| Member | Summary |
| --- | --- |
| `public NotificationHandlerRunBeforeAttribute(Type handlerType)` |  |
| `public Type HandlerType` | Gets the handler type the annotated handler must run before. |

## Interlocutor.Abstractions.Pipeline

### `AuditEntry` record

Represents a single audit record written by `AuditingBehavior`.

| Member | Summary |
| --- | --- |
| `public AuditEntry(string ActionName, string RequestType, string Outcome, DateTime Timestamp, string? UserId = null)` |  |
| `public string ActionName` | The logical action name (e.g., "User.Create", "Order.Cancel") from `ActionName`. |
| `public string Outcome` | A short outcome description (e.g., "Success", "Forbidden", "NotFound"), produced by `IResponseOutcomeDescriber`. |
| `public string RequestType` | The request type name, used for categorizing audit entries. |
| `public DateTime Timestamp` | The UTC timestamp when the request was processed. |
| `public string? UserId` | The authenticated user ID from request context, or null if anonymous. |
| `public void Deconstruct(out string ActionName, out string RequestType, out string Outcome, out DateTime Timestamp, out string? UserId)` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(AuditEntry? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(AuditEntry? left, AuditEntry? right)` |  |
| `public static bool operator !=(AuditEntry? left, AuditEntry? right)` |  |

### `AuditingBehavior<TRequest, TResponse>` class

Pipeline behavior that records an audit trail for every request it runs for.

| Member | Summary |
| --- | --- |
| `public AuditingBehavior(IAuditSink sink, IResponseOutcomeDescriber? describer = null, IRequestContextAccessor? contextAccessor = null)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, recording an audit entry after the handler completes. |

### `AuthorizationBehavior<TRequest, TResponse>` class

Pipeline behavior that enforces declarative authorization policies.

| Member | Summary |
| --- | --- |
| `public AuthorizationBehavior(IPolicyEvaluator policyEvaluator, IOutcomeCaseRegistry caseRegistry)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, evaluating the authorization policy and short-circuiting with `Forbidden` if it fails. |

### `CancellationBehavior<TRequest, TResponse>` class

Pipeline behavior that converts cooperative cancellation of the ambient request into the `Cancelled` outcome case.

| Member | Summary |
| --- | --- |
| `public CancellationBehavior(IOutcomeCaseRegistry caseRegistry)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, converting a cancellation of the ambient `ct` observed while awaiting `next` into a `Cancelled` outcome. |

### `DefaultPolicyEvaluator` sealed class

Default `IPolicyEvaluator` implementation suitable for development and testing.

| Member | Summary |
| --- | --- |
| `public DefaultPolicyEvaluator()` |  |
| `public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct)` |  |
| `public static readonly DefaultPolicyEvaluator Instance` | Gets the singleton instance. |

### `DefaultRateLimiter` sealed class

Default `IRateLimiter` implementation suitable for development and testing.

| Member | Summary |
| --- | --- |
| `public DefaultRateLimiter()` |  |
| `public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct)` |  |
| `public static readonly DefaultRateLimiter Instance` | Gets the singleton instance. |

### `DefaultResponseOutcomeDescriber` sealed class

Default `IResponseOutcomeDescriber`. Uses an `IOutcomeClassifier` to produce labels. Unwraps compiler-generated unions via `IUnion` before labeling.

| Member | Summary |
| --- | --- |
| `public DefaultResponseOutcomeDescriber(IOutcomeClassifier classifier)` | Initializes a new instance of the `DefaultResponseOutcomeDescriber` class. Initializes a new instance with the given outcome classifier. |
| `public string Describe(object? response)` |  |

### `DefaultTimeoutPolicy` sealed class

Default `ITimeoutPolicy` implementation that leaves each request's declared budget unchanged.

| Member | Summary |
| --- | --- |
| `public DefaultTimeoutPolicy()` |  |
| `public TimeSpan ResolveBudget(ITimeoutRequest request)` |  |
| `public static readonly DefaultTimeoutPolicy Instance` | Gets the singleton instance. |

### `DelegatePipelineStage<TRequest, TResponse>` sealed class

An `IPipelineStage` implementation that wraps caller-supplied delegates instead of requiring a dedicated class - the type backing lambda-form stage registration (for example, InterlocutorPipelineBuilder.AddStage's delegate overloads).

| Member | Summary |
| --- | --- |
| `public DelegatePipelineStage(Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore, Func<TRequest, TResponse, CancellationToken, Task>? onAfter)` |  |
| `public override Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct)` |  |
| `public override Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct)` |  |

### `DelegateRateLimiter` sealed class

An `IRateLimiter` implementation that wraps a caller-supplied delegate instead of requiring a dedicated class - useful for inline configuration in tests or small applications.

| Member | Summary |
| --- | --- |
| `public DelegateRateLimiter(Func<string, object, CancellationToken, Task<RateLimitResult>> check)` |  |
| `public Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct)` |  |

### `DelegateTimeoutPolicy` sealed class

`ITimeoutPolicy` implementation that delegates budget resolution to a user-provided function.

| Member | Summary |
| --- | --- |
| `public DelegateTimeoutPolicy(Func<ITimeoutRequest, TimeSpan> resolveBudget)` |  |
| `public TimeSpan ResolveBudget(ITimeoutRequest request)` |  |

### `Eject` class

Forces a pipeline dispatch to end immediately with a developer-chosen outcome, from anywhere inside a handler's or pipeline behavior's own call graph.

| Member | Summary |
| --- | --- |
| `public static TResponse With<TResponse>(TResponse response)` | Ejects the current pipeline dispatch, forcing it to end with `response` instead of whatever the calling code would otherwise have produced. |

### `IAuditSink` interface

Persists audit records produced by `AuditingBehavior`.

| Member | Summary |
| --- | --- |
| `Task RecordAsync(AuditEntry entry, CancellationToken ct)` | Writes an audit entry to the backing store. |

### `IPipelineBehavior<TRequest, TResponse>` interface

Defines a decorator that intercepts a request before or after its handler executes.

| Member | Summary |
| --- | --- |
| `Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Processes the request and invokes the next handler in the pipeline. |

### `IPipelineOverrideRegistry` interface

Resolves which explicitly-declared, per-request-type override pipeline - if any - replaces the general-purpose baseline for a given request type, at the point a dispatch's behavior chain is built.

| Member | Summary |
| --- | --- |
| `bool TryGetOverride(Type requestType, out IReadOnlyList<Type> behaviorTypes)` | Attempts to find the override rule that applies to `requestType`. |

### `IPipelineStage<TRequest, TResponse>` interface

Lightweight pre/post hook interface without creating a full `IPipelineBehavior`.

| Member | Summary |
| --- | --- |
| `Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct)` | Invoked after the request has been handled and a result is available - either the handler's result, or the response a before-hook short-circuited with. Not invoked when the handler throws. |
| `Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct)` | Invoked before the request enters the inner pipeline. Returning a short-circuited `StageResult` skips the handler and any remaining before-hooks; the short-circuit response then unwinds through OnAfterAsync for every stage whose before-hook already ran, including this one, before flowing out as an ordinary response. |

### `IPolicyEvaluator` interface

Minimal authorization abstraction that avoids coupling to ASP.NET Core's IAuthorizationService.

| Member | Summary |
| --- | --- |
| `Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct)` | Evaluates whether the request is authorized for the given policy. |

### `IRateLimiter` interface

Minimal rate-limiting abstraction that avoids coupling to any specific throttling library.

| Member | Summary |
| --- | --- |
| `Task<RateLimitResult> CheckAsync(string limitKey, object request, CancellationToken ct)` | Checks whether the request is allowed to proceed under the named limit. |

### `IResponseOutcomeDescriber` interface

Inspects a handler's response generically for cross-cutting concerns (logging, auditing) without requiring the response to be any specific type.

| Member | Summary |
| --- | --- |
| `string Describe(object? response)` | Produces a short, human-readable label describing the response's outcome. |

### `ITimeoutPolicy` interface

Resolves the effective time budget for a request implementing `ITimeoutRequest`.

| Member | Summary |
| --- | --- |
| `TimeSpan ResolveBudget(ITimeoutRequest request)` | Resolves the effective time budget for `request`. |

### `LoggingAuditSink` sealed class

Default `IAuditSink` implementation that records audit entries through the logging abstraction.

| Member | Summary |
| --- | --- |
| `public LoggingAuditSink(ILogger<LoggingAuditSink> logger)` |  |
| `public Task RecordAsync(AuditEntry entry, CancellationToken ct)` |  |

### `LoggingBehavior<TRequest, TResponse>` class

Pipeline behavior that logs every mediator request passing through the pipeline.

| Member | Summary |
| --- | --- |
| `public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IResponseOutcomeDescriber? describer = null)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, logging the outcome and elapsed time. |

### `PipelineChainResolver` class

Resolves the ordered `IPipelineBehavior` list for one dispatch - the single decision point every dispatch path consults so a per-request-type pipeline override is enforced identically and exactly once, regardless of which path a request travels.

| Member | Summary |
| --- | --- |
| `public static IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> ResolveChain<TRequest, TResponse>(IServiceProvider services)` | Resolves the ordered behavior list for `TRequest`/`TResponse`. |

### `PipelineEjectException<TResponse>` sealed class

Internal control-flow signal thrown by `With` to force a pipeline dispatch to end immediately with a developer-chosen `TResponse`, from anywhere inside a handler's or pipeline behavior's own call graph.

| Member | Summary |
| --- | --- |
| `public TResponse Response` | Gets the outcome the ejecting dispatch should return. |

### `PipelineStageBase<TRequest, TResponse>` abstract class

Convenience base class for `IPipelineStage` implementations that only need one of the two hooks. Both hooks default to a no-op, so a derived class overrides only the one it needs.

| Member | Summary |
| --- | --- |
| `public virtual Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct)` | Invoked after the request has been handled (or short-circuited). The default implementation is a no-op - override to tear down state set up in `OnBeforeAsync`. |
| `public virtual Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct)` | Invoked before the request enters the inner pipeline. The default implementation is a no-op that always continues - override to inspect, short-circuit, or set up state for `OnAfterAsync`. |

### `PipelineStageBehavior<TRequest, TResponse>` class

Aggregates all registered `IPipelineStage` instances and invokes them around the handler call.

| Member | Summary |
| --- | --- |
| `public PipelineStageBehavior(IEnumerable<IPipelineStage<TRequest, TResponse>> stages, ILogger<PipelineStageBehavior<TRequest, TResponse>> logger, bool rethrowAfterHookExceptions = false)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, invoking registered pipeline stages around the handler call. |

### `RateLimitResult` struct

The outcome of an `IRateLimiter` check.

| Member | Summary |
| --- | --- |
| `public RateLimitResult(bool IsAllowed, TimeSpan? RetryAfter = null)` |  |
| `public bool IsAllowed` | `true` if the request is allowed to proceed; otherwise `false`. |
| `public TimeSpan? RetryAfter` | An optional hint for how long the caller should wait before retrying, propagated into the `RateLimited` case's own RetryAfter when `IsAllowed` is `false`. |
| `public void Deconstruct(out bool IsAllowed, out TimeSpan? RetryAfter)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(RateLimitResult other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(RateLimitResult left, RateLimitResult right)` |  |
| `public static bool operator !=(RateLimitResult left, RateLimitResult right)` |  |

### `RateLimitedBehavior<TRequest, TResponse>` class

Pipeline behavior that enforces rate limits.

| Member | Summary |
| --- | --- |
| `public RateLimitedBehavior(IRateLimiter rateLimiter, IOutcomeCaseRegistry caseRegistry)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, checking the rate limit and short-circuiting with `RateLimited` if it's exceeded. |

### `StageResult<TResponse>` struct

The outcome of a pipeline stage's before-hook: either "continue" (the handler and any remaining before-hooks proceed normally) or "short-circuit" (the handler and any remaining before-hooks are skipped, and `Response` becomes the pipeline's result).

| Member | Summary |
| --- | --- |
| `public static StageResult<TResponse> Continue` | Gets a result that lets the request continue through the pipeline unchanged. |
| `public TResponse Response` | Gets the response to short-circuit with. Meaningful only when `ShortCircuited` is `true`. |
| `public bool ShortCircuited` | Gets a value indicating whether the before-hook has short-circuited the pipeline. |
| `public static StageResult<TResponse> ShortCircuit(TResponse response)` | Creates a result that stops the pipeline and returns `response` immediately. |

### `TimeoutBehavior<TRequest, TResponse>` class

Pipeline behavior that enforces a time budget by racing the rest of the pipeline against a delay.

| Member | Summary |
| --- | --- |
| `public TimeoutBehavior(ITimeoutPolicy timeoutPolicy, IOutcomeCaseRegistry caseRegistry)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Handles the request, racing it against the resolved time budget and short-circuiting with `Timeout` if the budget elapses first. |

### `UnionCaseFactory` class

Resolves and invokes the constructor the C# union feature generates for one case type of a union, so a short-circuiting behavior can construct its failure case for a caller-chosen response type whose concrete shape isn't known at compile time.

| Member | Summary |
| --- | --- |
| `public static ConstructorInfo? FindCaseConstructor<TResponse, TCase>()` | Finds `TResponse`'s constructor accepting a single `TCase` argument - the case constructor the union feature generates when `TResponse` declares `TCase` as one of its case types. |
| `public static TResponse Invoke<TResponse, TCase>(ConstructorInfo caseConstructor, TCase caseValue)` | Invokes a case constructor previously resolved by `FindCaseConstructor`, constructing the union's `TCase` case from `caseValue`. |

## Interlocutor.Abstractions.Pipeline.Cases

### `DefaultOutcomeCaseRegistry` sealed class

Zero-config `IOutcomeCaseRegistry` that handles any `IOutcome` which is a compiler-generated union: construction via `UnionCaseFactory`'s case-constructor reflection, extraction via pattern-matching the compiler-generated `IUnion` marker interface.

| Member | Summary |
| --- | --- |
| `public DefaultOutcomeCaseRegistry()` |  |
| `public static IOutcomeCaseRegistry Instance` | Gets the shared default registry instance. |
| `public TResponse Construct<TResponse, TCase>(TCase caseValue)` |  |
| `public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)` |  |

### `IOutcomeCaseRegistry` interface

Constructs an `IOutcome` response from one of its case values, and extracts the active case type/value back out of an existing response, without either side knowing whether the response is a compiler-generated union or a hand-rolled `IOutcome` implementation.

| Member | Summary |
| --- | --- |
| `TResponse Construct<TResponse, TCase>(TCase caseValue)` | Constructs a `TResponse` whose active case is `caseValue`. |
| `bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)` | Extracts the active case type and value from `response`. |

### `OutcomeCaseExtractor<TResponse>` delegate

Extracts the active case type/value from a `TResponse` for `RegisterExtractor`.

| Member | Summary |
| --- | --- |
| `public OutcomeCaseExtractor(object @object, nint method)` |  |
| `public virtual IAsyncResult BeginInvoke(TResponse response, out Type caseType, out object? caseValue, AsyncCallback callback, object @object)` |  |
| `public virtual bool EndInvoke(out Type caseType, out object? caseValue, IAsyncResult result)` |  |
| `public virtual bool Invoke(TResponse response, out Type caseType, out object? caseValue)` |  |

### `OutcomeCaseNotSupportedException` sealed class

Thrown by `Construct` or `TryGetActiveCase` when the registry has no way to construct or decompose a given response type - it is neither a compiler-generated union covering the case in question, nor covered by a factory/extractor registered via `OutcomeCaseRegistryBuilder`.

| Member | Summary |
| --- | --- |
| `public OutcomeCaseNotSupportedException(Type responseType, Type? caseType, string message)` | Initializes a new instance of the `OutcomeCaseNotSupportedException` class. |
| `public Type? CaseType` | Gets the case type that could not be constructed or extracted, or `null` when the failure is extraction of an unknown active case rather than construction of a specific one. |
| `public Type ResponseType` | Gets the `IOutcome` type that could not be constructed or decomposed. |

### `OutcomeCaseRegistryBuilder` sealed class

Builds an immutable `IOutcomeCaseRegistry`, mirroring `OutcomeClassifierBuilder`'s shape: a consumer registers a custom factory and/or extractor for their own `IOutcome` implementation, and anything left unregistered falls back to `DefaultOutcomeCaseRegistry`'s reflection-based union handling.

| Member | Summary |
| --- | --- |
| `public OutcomeCaseRegistryBuilder()` |  |
| `public IOutcomeCaseRegistry Build()` | Builds an immutable `IOutcomeCaseRegistry` from the registrations. |
| `public OutcomeCaseRegistryBuilder RegisterExtractor<TResponse>(OutcomeCaseExtractor<TResponse> extractor)` | Registers a custom extractor that reads the active case type/value out of a `TResponse`. |
| `public OutcomeCaseRegistryBuilder RegisterFactory<TResponse, TCase>(Func<TCase, TResponse> factory)` | Registers a custom factory that constructs a `TResponse` from a `TCase` case value. |

### `OutcomeCaseRegistryExtensions` class

Convenience extensions over `IOutcomeCaseRegistry` for pipeline behaviors that short-circuit a request with a specific outcome case (Forbidden, Cancelled, Timeout, RateLimited, ValidationErrors, ...).

| Member | Summary |
| --- | --- |
| `public static TResponse ConstructOrThrow<TResponse, TCase>(IOutcomeCaseRegistry caseRegistry, TCase caseValue, string behaviorName, string eventPhrase, string requirementClause)` | Constructs a `TResponse` whose active case is `caseValue`, or throws a behavior-specific `InvalidOperationException` if `TResponse` has no `TCase` case to construct. |

## Interlocutor.Abstractions.Pipeline.Classifiers

### `IOutcomeClassifier` interface

Labels operation outcomes for logging/auditing, by registered type, predicate, or fallback.

| Member | Summary |
| --- | --- |
| `string GetLabel(object? response)` | Gets a human-readable label for the outcome. |

### `OutcomeClassifierBuilder` sealed class

Builds an immutable `IOutcomeClassifier` that labels operation outcomes for logging/auditing. Seeded with all kernel types via open-generic matching; consumers may register additional custom types.

| Member | Summary |
| --- | --- |
| `public OutcomeClassifierBuilder()` | Initializes a new instance of the `OutcomeClassifierBuilder` class. Initializes a new builder, pre-seeded with all Interlocutor.CommonKernel case types. |
| `public static IOutcomeClassifier DefaultClassifier` | Gets the shared default classifier instance, pre-seeded with all kernel types. |
| `public IOutcomeClassifier Build()` | Builds an immutable classifier from the registrations. |
| `public OutcomeClassifierBuilder RegisterPredicate(Func<object?, bool> predicate, string label)` | Registers a predicate-based label for outcome types that cannot be expressed by type alone. |
| `public OutcomeClassifierBuilder RegisterType<T>(string label)` | Registers a type's display label. |
| `public OutcomeClassifierBuilder RegisterType(Type type, string label)` | Registers a type's display label, by runtime `Type`. |

## Interlocutor.Abstractions.Pipeline.Mapping

### `IOutcomeMapper<TResult>` interface

Maps a failure case value from an Interlocutor Outcome union to a `TResult` representing it in some transport or output format - an HTTP status and body, a gRPC status, a process exit code, a queue nack reason, or anything else a consumer needs to derive from a failure case.

| Member | Summary |
| --- | --- |
| `TResult Map<TFailure>(TFailure failure)` | Maps a failure case value to its `TResult` representation. |

### `OutcomeMapperBase<TResult>` abstract class

Recognizes the built-in ResultTypes failure cases and their aggregate forms, dispatching each to its own protected abstract method - a concrete `TResult`- specific mapper (e.g. DefaultHttpOutcomeStatusMapper) implements one method per case instead of writing the dispatch itself. Anything else - including a consumer's own custom failure type - falls back to `MapUnrecognized`.

| Member | Summary |
| --- | --- |
| `public TResult Map<TFailure>(TFailure failure)` |  |

## Interlocutor.Abstractions.Queries

### `IQuery<TResponse>` interface

Marks a type as a CQRS query and defines the type of data produced when the query is handled.


### `IQueryHandler<TQuery, TResponse>` interface

Defines a handler that processes a CQRS query of a specified type and returns a result.

| Member | Summary |
| --- | --- |
| `Task<TResponse> Handle(TQuery query, CancellationToken ct)` | Handles the specified query and returns a result that represents success or failure. |
