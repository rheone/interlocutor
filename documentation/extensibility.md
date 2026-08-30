# Extensibility

A map of every point where a consumer plugs their own type into Interlocutor, and where to find the deep dive on each. None of these are required - the library works fully without any of them; they exist for the cases the built-in mechanism doesn't cover.

## Marker interfaces: the general pattern

A request opts into a cross-cutting pipeline behavior by implementing a marker interface - never by editing a central configuration list, and never by the behavior inspecting the request's concrete type. The shipped markers (`ITransactionalRequest`, `IExternalStoreRequest`, `IAuditableRequest`, `IRequiresPolicy`, `ICancellable`, `IHasTimeout`, `IRateLimited` - see [Glossary § Marker Interfaces](glossary.md#marker-interfaces)) are all built the same way, and your own pipeline behavior can follow the identical shape:

```csharp
// 1. Declare the marker - a plain interface, no base type required
public interface IIdempotent
{
    string IdempotencyKey { get; }
}

// 2. Constrain your behavior's TRequest to it
public sealed class IdempotencyBehavior<TRequest, TResponse>(IIdempotencyStore store)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotent
    where TResponse : IOperationOutcome
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        if (await store.TryGetCachedResponseAsync<TResponse>(request.IdempotencyKey, ct) is { } cached)
        {
            return cached;
        }

        var response = await next();
        await store.RecordAsync(request.IdempotencyKey, response, ct);
        return response;
    }
}

// 3. A request opts in simply by implementing the marker
public record ChargeCard(string IdempotencyKey, decimal Amount) : ICommand<OperationOutcome<Receipt, Failure>>, IIdempotent;
```

Register it as an open generic, exactly like a built-in behavior - the container only constructs `IdempotencyBehavior<,>` for a request type that satisfies the `where TRequest : IIdempotent` constraint:

```csharp
builder.Pipeline.AddBehavior(typeof(IdempotencyBehavior<,>));
```

See [Pipeline & Behavior Composition](pipeline-and-behaviors.md) for the full behavior-composition model this plugs into, and [Registering Custom Behaviors](pipeline-and-behaviors.md#registering-custom-behaviors) for the general `IPipelineBehavior<TRequest, TResponse>` contract.

## Lightweight hooks: `IPipelineStage`

When a behavior only needs a before/after hook - no need to own the `next()` delegate - `IPipelineStage<TRequest, TResponse>` (via `PipelineStageBase<TRequest, TResponse>`) is the lighter alternative, registered via `builder.Pipeline.AddStage(...)`/`.AddStageHost()`. See [Pipeline & Behavior Composition § Pipeline Stages](pipeline-and-behaviors.md#pipeline-stages-ipipelinestage) for the full contract, including short-circuiting.

## Custom `IOperationOutcome` implementations

`IOperationOutcome` is a bare marker - any type can implement it, not only the library's own compiler-generated `OperationOutcome<...>` unions. A hand-rolled response type is legal anywhere a handler response is expected:

```csharp
public sealed class PaymentOutcome : IOperationOutcome
{
    private PaymentOutcome(Type caseType, object? caseValue) => (this.CaseType, this.CaseValue) = (caseType, caseValue);

    public Type CaseType { get; }

    public object? CaseValue { get; }

    public static PaymentOutcome FromSuccess(Receipt value) => new(typeof(Receipt), value);

    public static PaymentOutcome FromFailure<TFailure>(TFailure failure) where TFailure : notnull => new(typeof(TFailure), failure);
}
```

What makes this usable by the pipeline isn't implementing some richer interface - it's being teachable to `IOutcomeCaseRegistry`, the construction/extraction seam `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, and `Interlocutor.FluentValidation`'s `ValidationBehavior` all go through instead of constructing a case directly. `OverrideOutcomeCaseRegistry<TRegistry>()` takes a type, constructed through DI like any other service - not a pre-built instance - so wrap the `OutcomeCaseRegistryBuilder` output in a small class:

```csharp
public sealed class PaymentOutcomeCaseRegistry : IOutcomeCaseRegistry
{
    private static readonly IOutcomeCaseRegistry Inner = new OutcomeCaseRegistryBuilder()
        .RegisterFactory<PaymentOutcome, Forbidden>(PaymentOutcome.FromFailure)
        .RegisterExtractor<PaymentOutcome>((PaymentOutcome response, out Type caseType, out object? caseValue) =>
        {
            caseType = response.CaseType;
            caseValue = response.CaseValue;
            return true;
        })
        .Build();

    public TResponse Construct<TResponse, TCase>(TCase caseValue) where TResponse : IOperationOutcome =>
        Inner.Construct<TResponse, TCase>(caseValue);

    public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
        where TResponse : IOperationOutcome =>
        Inner.TryGetActiveCase(response, out caseType, out caseValue);
}

builder.Pipeline.OverrideOutcomeCaseRegistry<PaymentOutcomeCaseRegistry>();
```

Anything left unregistered still falls back to `DefaultOutcomeCaseRegistry`, so you only register the response types that aren't compiler-generated unions. See [Messages, Result Types & Variance § IOperationOutcome as an Extension Point](messages-and-result-types.md#ioperationoutcome-as-an-extension-point) for the full walkthrough.

A related but separate extension point: `LoggingBehavior`, `AuditingBehavior`, and both store-specific `UnitOfWorkBehavior` implementations need to describe a response for a log/audit line and classify it as success or failure, which is a different question from constructing/extracting a case. That's `IResponseOutcomeDescriber`, built on `OutcomeClassifierBuilder` - see [Pipeline & Behavior Composition § Outcome Description & Classification](pipeline-and-behaviors.md#outcome-description--classification) for the worked example. And at the HTTP boundary, `IOutcomeStatusMapper` maps a failure case to a status code and body - see [ASP.NET Core Integration § Custom HTTP Mapping](aspnetcore-integration.md#custom-http-mapping).

## Full-fidelity failure cases: `IValidationFailureCase` and its siblings

`Interlocutor.FluentValidation.ValidationBehavior` collapses every validator's failures down to
`ValidationErrors` - a message-only aggregate, portable across any validation technology. A handler that
needs more - FluentValidation's `PropertyName`, `Severity`, `ErrorCode`, `CustomState`, and `AttemptedValue` -
opts into `FluentValidationErrors` instead, by declaring it as a case and registering
`FluentValidationErrorsBehavior` in place of `ValidationBehavior`:

```csharp
public record CreateUser(string Name, string Email)
    : ICommand<OperationOutcome<int, FluentValidationErrors>>;

builder.Pipeline.AddBehavior(typeof(FluentValidationErrorsBehavior<,>));
```

`FluentValidationErrors` wraps every `FluentValidation.Results.ValidationFailure` verbatim - nothing is
projected away - while still implementing [`IValidationFailureCase`](glossary.md#result-types), the same
minimal interface `ValidationErrors` implements. A consumer that recognizes `IValidationFailureCase` - such as
`DefaultOutcomeStatusMapper` (see [ASP.NET Core Integration § Status Code Mapping](aspnetcore-integration.md#status-code-mapping))
- handles either case type the same way, without needing to know which validation technology produced it.
This is the same extension-point shape as a custom `IOperationOutcome` implementation above: a minimal marker
interface a technology-specific case type implements, rather than a base type every case would need to
inherit from - record structs can't be inherited from at all, which is exactly why this couldn't be modeled
as `FluentValidationErrors : ValidationErrors`.

`RateLimited`, `Timeout`, and `Forbidden` follow the identical shape, one interface each -
`IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IForbiddenFailureCase` - and `DefaultOutcomeStatusMapper`
recognizes all three generically today, the same way it recognizes `IValidationFailureCase`. Unlike
validation, no second implementation ships yet: `Interlocutor.Abstractions` has exactly one `IRateLimiter`,
one `ITimeoutPolicy`, and one `IPolicyEvaluator`. A consumer who implements one of those three interfaces with
a technology-specific diagnostic shape - a distributed rate limiter's bucket state, a Polly policy's context,
which of several authorization policies denied a request - can pair it with their own case type implementing
the matching marker interface, and `DefaultOutcomeStatusMapper` maps it correctly with no override required.

## Predicate-based registration: `InterlocutorBuilder.AddTypeMatch`

Marker interfaces are the idiomatic opt-in mechanism, but sometimes what should be registered as a handler or behavior is better expressed as a predicate over discovered types than a shared interface - a namespace convention, a naming convention, anything scanning already sees:

```csharp
builder.ScanAssembly(typeof(Program).Assembly);
builder.AddTypeMatch(
    predicate: t => t.Namespace == "MyApp.Audit",
    target: TypeMatchTarget.Handler,
    except: t => t == typeof(LegacyAuditHandler),
    require: true,   // ValidateOnBuild() fails the build if this matches nothing
    name: "audit-handlers"
);
```

This is strictly additive to marker-interface-based scoping - both mechanisms work independently and simultaneously, and a match rule's registrations show up in `DescribeRegistrations()` tagged with `Provenance.Matched` and the rule's `name`. See [Builder & Registration § Registering by Predicate](builder-and-registration.md#registering-by-predicate).

## Extending ambiguity checking to your own contracts

The "at most one implementation per closed instantiation" check the three built-in mediator handler contracts get automatically can be opted into for your own open-generic contract, with `Eject` as the escape hatch for a genuine conflict. See [Builder & Registration § Extending Ambiguity Checking to Your Own Contracts](builder-and-registration.md#extending-ambiguity-checking-to-your-own-contracts) - this is the mechanism to reach for if your application defines its own "exactly one implementation" open-generic contract (a formatter, a strategy resolver, anything with the same shape as a handler) and wants the same build-time guarantee.

## Replacing the pipeline entirely for specific request types

Every mechanism above adds to or filters the general-purpose pipeline. `InterlocutorPipelineBuilder.OverridePipelineFor` is different in kind: it declares a full, positively-specified replacement pipeline for a request type (or a predicate matching several), running **instead of** the general-purpose baseline rather than alongside it:

```csharp
builder.Pipeline
    .UseRecommendedChain()
    .OverridePipelineFor<ArchiveOrder>("archive-order-pipeline", typeof(ArchivalAuditBehavior<,>));
```

Reach for this when a request type's needs diverge enough from the baseline that adding markers to skip pieces of it would be more confusing than declaring what it actually needs. See [Pipeline & Behavior Composition § Per-Request-Type Pipeline Overrides](pipeline-and-behaviors.md#per-request-type-pipeline-overrides) for the full model, including why there's deliberately no "skip"/"exclude" primitive alongside it.

## Where each hook is invoked

For tracing the mechanism rather than copying an example:

| Extension point | Declared in | Invoked from |
|---|---|---|
| Marker interfaces | Your own request type | The container's generic-constraint resolution - no Interlocutor code inspects markers directly |
| `IPipelineBehavior<,>` / `IPipelineStage<,>` | `Interlocutor.Abstractions.Pipeline` | `Mediator`'s dispatch path, chained via `InterlocutorPipelineBuilder` |
| `IOutcomeCaseRegistry` | `Interlocutor.Abstractions.Pipeline.Cases` | `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, `Interlocutor.FluentValidation.ValidationBehavior`, `Interlocutor.FluentValidation.FluentValidationErrorsBehavior` |
| `IOutcomeClassifier` / `IResponseOutcomeDescriber` | `Interlocutor.Abstractions.Pipeline.Classifiers` | `LoggingBehavior`, `AuditingBehavior`, both `UnitOfWorkBehavior` implementations |
| `IOutcomeStatusMapper` | `Interlocutor.AspNetCore` | `ResultExtensions.ToActionResult`/`ToMinimalApiResult` |
| `InterlocutorBuilder.AddTypeMatch` / `RequireUniqueClosure` / `Eject` | `Interlocutor.DependencyInjection.Abstractions` | Resolved when `AddInterlocutor` translates the accumulated registration list; checked by `ValidateOnBuild()` |
| `InterlocutorPipelineBuilder.OverridePipelineFor` | `Interlocutor.DependencyInjection.Abstractions` | `PipelineChainResolver`, at the point each dispatch path builds its behavior chain |
