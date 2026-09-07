# Conversation Export - C# 15 Union-Based Mediator Result Design

*Exported conversation. Code artifacts created during the session are not reproduced here - only the discussion itself.*

---

## 1. Initial design question

**User:**

> Give the the new C# union type in .Net 11 <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union>
>
> what is an appropriate structure, and types of the union for a mediator result type (exceptions should only be thrown from exceptional events, and should not not normally be thrown by user action. That would be a bug, which is exceptional.
>
> Presumably one item in the union would be a generic type T that represents a result of a result exists assuming there is a result to return / some flag that states success for a command
>
> As it is a database / logic driven it should potentially have a failed validation type with a map of reasons (error code, human readable message)
>
> States like not found, not authorized, conflict
>
> What am I not account for that would typically be used and why. Am I going about this in the right way?
>
> What shape should this union and it's components take? I can't think of a reason why it would be mutable after creation. Nor do I want it to reflect http codes directly even if this will be part of a restful API.
>
> Likely the queries and commands will be related to a database entiry.
>
> Handlers will be wrapped by other handlers to accomplish pre/post actions such as database look up, validation logic, etc.
>
> What are your suggestions? And why? A couple of options may be good. What am I missing with my approach?

**Claude:**

Quick correction on the source first: this is real - C# unions shipped as a language feature but the runtime pieces (`UnionAttribute`/`IUnion`) aren't in .NET 11 Preview 2 yet, so today you declare the plumbing yourself (the doc gives the exact boilerplate). Worth knowing since it affects how portable your code is across preview builds.

### What you're missing, and why

**Authentication vs authorization.** "Not authorized" is really two different things: *not authenticated* (401-equivalent) and *authenticated but forbidden* (403-equivalent). Split into `Unauthorized` and `Forbidden`. *(Superseded in a later turn - see §2 below: authentication turned out to happen entirely upstream, so `Unauthorized` was dropped.)*

**Business-rule violations distinct from field validation.** Field validation ("email is malformed") is a precondition check on the input shape. A business rule violation ("account balance insufficient") is a domain invariant failing after the input was structurally valid. Cramming both into `ValidationFailed` makes the error map ambiguous. Added a distinct `RuleViolation` (or `Error`) case with a code/message, separate from the per-field map.

**A no-payload success case.** Not every command returns a `T`. Need a non-generic result too, not just `Result<T>`.

**Concurrency conflicts specifically.** "Conflict" as a bucket is fine, but optimistic-concurrency failures (stale ETag/RowVersion) usually want to carry the current version so the caller can decide to retry/merge.

**Identity info on `NotFound`.** A bare marker loses debuggability. Carry entity type + key internally (for logs/telemetry) even if the API layer strips it before serializing to the client.

**What you correctly excluded, and rightly so:** cancellation, timeouts, infra failures - these are legitimately exceptional and should throw. Don't model them in the union.

### Two shapes, and the tradeoff

**Option A - flat union.** All cases listed directly on `Result<T>`:

```csharp
public record ValidationFailed(IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> Errors);
public record RuleViolation(string Code, string Message);
public record NotFound(string EntityType, object Key);
public record Unauthorized;
public record Forbidden(string? Reason = null);
public record Conflict(string Code, string Message);

public union Result<T>(T, ValidationFailed, RuleViolation, NotFound, Unauthorized, Forbidden, Conflict);
public union Result(Success, ValidationFailed, RuleViolation, NotFound, Unauthorized, Forbidden, Conflict);
public record Success; // marker for no-payload commands
```

Every failure record converts implicitly and directly into `Result<T>` - one hop, no ceremony in handlers.

**Option B - composed union.** Pull the failure cases into their own union, nested as a single case type:

```csharp
public union Failure(ValidationFailed, RuleViolation, NotFound, Unauthorized, Forbidden, Conflict);
public union Result<T>(T, Failure);
public union Result(Success, Failure);
```

Unions can be case types of other unions. Less duplication, and decorators can work in terms of `Failure` alone. The catch: C# doesn't chain two user-defined implicit conversions, so `ValidationFailed → Failure → Result<T>` won't happen in one assignment - needs an explicit two-step hop, or a small helper extension.

**Recommendation:** go with B, especially given the decorator-pipeline architecture (lookup → validation → handler) - a shared `Failure` type earns its keep there.

### Shape/mutability

- Case types should be `record` (or `record struct` for small ones) for value equality and `with`-expressions.
- Prefer the compiler-generated `union` struct form unless you specifically need reference semantics.
- One real cost: struct unions box everything into `object?`, including value-type `T`. Usually negligible for a mediator layer.

### Not modeling toward HTTP

Keep `Result<T>`/`Failure` HTTP-agnostic; translate once, at the API boundary:

```csharp
IActionResult ToActionResult<T>(Result<T> result) => result.Value switch
{
    T ok => Ok(ok),
    ValidationFailed v => BadRequest(v.Errors),
    RuleViolation r => UnprocessableEntity(r),
    NotFound => NotFound(),
    Unauthorized => Unauthorized(),
    Forbidden => Forbid(),
    Conflict c => Conflict(c),
};
```

Exhaustive, so adding a case later flags every mapping site - the point of reaching for a union here.

---

## 2. Correction and refinement - auth timing, read models, pre/post validation

**User:**

> .net 11 preview 3 with c# 15 has unions, though the syntax may not be finalized. See included link
>
> Authentication will happen earlier and elsewhere, way before this. An unauthorize user would not be able to make any action.
>
> The authorization I may have mentioned would have been around the authorization to make specific resource based authorization when auth coule bit be made earlier in the controller call.
>
> For queries with a return I anticipate a read only mapped graph of the necessary domain entity
>
> field validation / input validation would be different than business logic validation
>
> Result of T would be the type of a query with a reaponse, the query should know what is being rerurned on success is a type T
>
> A pre/post validation could help with reaource based decisions as a pre validation can reject due to a bad shape, but a post validation, after entity lookup, could check the resource for ownership and decide to forbid based on missing claims or return T based on valid claims.
>
> For added complexity these queries and commands will be async in nature (or at least in shape) they should accept a cancellation token/default a cancellation token.
>
> Pattern matching / switch expressions will also be a big help. Especially at the controller level where the result can be easily mapped to a reusable "to http" response.
>
> Given this, what should my types looks like, how might they be processed, and fed out to the API.
>
> What problems might I encounter?

**Claude:**

Confirmed - Preview 3 landed the same syntax I used, still marked non-final, and ASP.NET/minimal-API `TypedResults` integration isn't wired up yet.

### Types

Dropped `Unauthorized` (handled upstream), keeping only resource-based `Forbidden`:

```csharp
public record ValidationFailed(
    IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> Errors); // input shape

public record RuleViolation(string Code, string Message);       // domain/business logic
public record NotFound(string EntityType, object Key);
public record Forbidden(string? Reason = null);                 // resource-based authz, post-lookup
public record Conflict(string Code, string Message);

public union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);

public record Success;
public union Result<T>(T, Failure);   // queries, and commands that return something
public union Result(Success, Failure); // commands with no payload
```

`T` for queries is the read-only projection - a DTO/graph shaped for the query, not the entity itself.

### Handler contracts (async, cancellation-aware)

```csharp
public interface IQuery<TResponse> { }
public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}

public interface ICommand<TResponse> { }
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}
```

**Gotcha:** don't put `CancellationToken ct = default` on these interface methods - default parameter values on interfaces resolve at the call site's *static* type, not virtually, so calling through the interface vs. a concrete type can silently diverge. Put the default exactly once, at the outer boundary (`IMediator.Send`).

### Pipeline shape: two different concerns need two different mechanisms

Pre-validation (shape) needs no data from lookup, so a generic decorator is fine. Post-validation (resource ownership) needs the entity lookup just fetched - doing that with generic decorators requires a shared mutable context object, which is exactly the mutability to avoid.

Two ways to avoid it:

1. **Generic behaviors only for concerns that don't need fetched state** (input-shape validation, logging, transactions).
2. **A template-method base handler for lookup → resource-authorize → handle:**

```csharp
public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);
    protected abstract Failure? Authorize(TEntity entity, TCommand command); // null = allowed
    protected abstract Task<Result<TResponse>> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await LoadAsync(command, ct);
        if (entity is null) return new NotFound(typeof(TEntity).Name, /* id */ command);
        if (Authorize(entity, command) is { } denied) return denied;
        return await HandleAsync(command, entity, ct);
    }
}
```

Everything stays a plain return value passed down a call stack - no ambient state.

### Mapping to the API

Because `Failure` is its own union, the failure-to-HTTP mapping is written **once** and reused for every `Result<T>`/`Result`:

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) => result.Value switch
    {
        T ok => new OkObjectResult(ok),
        Failure f => f.ToActionResult(),
    };

    public static IActionResult ToActionResult(this Result result) => result.Value switch
    {
        Success => new NoContentResult(),
        Failure f => f.ToActionResult(),
    };

    public static IActionResult ToActionResult(this Failure failure) => failure.Value switch
    {
        ValidationFailed v => new BadRequestObjectResult(v.Errors),
        RuleViolation r => new UnprocessableEntityObjectResult(r),
        NotFound n => new NotFoundResult(),
        Forbidden => new ForbidResult(),
        Conflict c => new ConflictObjectResult(c),
    };
}
```

The top-level switch is two-armed (`T` / `Failure`) - detailed failure handling lives in the second switch. Adding a new `Failure` case later only requires updating `ToActionResult(Failure)`.

### Problems likely to be hit

- **Preview instability** - isolate `union` usage behind owned types so a syntax change is localized.
- **Struct boxing** of value-type `T` - usually irrelevant, worth knowing for hot paths.
- **No built-in serialization support** for unions - don't serialize `Result<T>`/`Failure` directly; map to DTOs/`IActionResult` at the boundary.
- **Scattered ad hoc switches** - funnel through one canonical `Failure -> X` switch per output format rather than duplicating logic.
- **`NotFound`/`Conflict` payload design** - decide structured fields up front; retrofitting a *case* later means touching every exhaustive switch (intended friction, but budget for it).

---

## 3. Decorator/pipeline naming, layering, and risks

**User:**

> This all seems reasonable. How would your recommendations the decorator pattern nesting around the handler? Maybe there is a better term for actions on the way in and actions on the way out.
>
> I know I'll want logging, auditing, read/write, shape / field validation, auth (many cases it will be a no-op for non resource based), or am I approaching this in the wrong way. Is wrapping fo< g to get me in trouble? Is there other "layers" I should account for? Presumably beyond specific business logic or validation rules and they type T all will be going through the same shape of handler. How to I keep the consistency of a generic type T returnable?
>
> Is there a better approach, something I'm missing?

**Claude:**

### Terminology

"Decorator" is fine informally, but the more precise terms are **pipeline behavior** and **middleware** - standard names for a decorator-style pipeline stage in this space generally. The mental model is the **onion / Russian-doll model**: each layer wraps the next, and *the same layer* handles both "on the way in" and "on the way out" - one behavior, one `next()` call in the middle:

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next);
}

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var sw = Stopwatch.StartNew();
        var result = await next();
        _logger.LogInformation("Handled {Request} in {Elapsed}ms -> {Outcome}",
            typeof(TRequest).Name, sw.ElapsedMilliseconds, result.Value switch { Failure f => f.Value.GetType().Name, _ => "Success" });
        return result;
    }
}
```

Because it's generic over `<TRequest, TResponse>`, **`T` never needs to be known by any behavior** - composition is mechanical, no per-request-type coupling.

### Recommended layering (outer → inner)

```
Tracing/telemetry span
  Logging
    Auditing (write path only)
      Shape/field validation
        Non-resource authorization (often no-op)
          Transaction / unit-of-work boundary (write path)
            Handler (lookup → resource authz → business logic → T)
```

- Validation before transaction - don't open a transaction for a request that fails shape validation.
- Auditing and transactions are write-path concerns - use marker interfaces (`IAuditableRequest`, `ITransactionalRequest`) rather than type-checking every request, so behaviors no-op cleanly for queries.
- Non-resource authorization is genuinely allowed to be a no-op - an `IRequiresPolicyRequest` marker, checked only if present.
- Resource-based authorization stays inside the handler's template method, not a behavior - same reasoning as before.

### Exceptions don't belong in the pipeline

Don't add an "exception → Failure" behavior - that quietly turns bugs into legitimate domain outcomes. Keep unexpected-fault handling outside the mediator pipeline entirely, at the ASP.NET level (`IExceptionHandler`), so it catches faults from any layer and turns them into a uniform 500/ProblemDetails response.

### Is wrapping going to get you in trouble?

- **Nothing enforces a request goes through the right behaviors** - mitigate with an architecture test or analyzer.
- **Registration order determines execution order**, and containers vary - pin it with a test.
- **Scoped services across the chain** - make sure the transaction behavior and handler resolve the same scoped `DbContext`.
- **Deep async call stacks** - .NET 11's Runtime Async V2 helps; keep the pipeline to ~6 layers.
- **Testing shifts** - unit-test handlers directly; integration-test the pipeline itself.

### Other layers worth planning for

- **Caching** (queries) - between authorization and handler.
- **Idempotency** (commands) - before opening a transaction.
- **Telemetry/tracing span** - outermost.
- **Resilience (retries/circuit breakers)** - belongs at the infrastructure layer (Polly around DB/HTTP clients), not the mediator pipeline - retrying a `NotFound`/`Forbidden` is meaningless.

The mental-model adjustment: don't think "pre-processors and post-processors" as two lists - one behavior wraps `next()`; reserve the template-method handler for anything needing the fetched entity.

---

## 4. PRD and ADR request

**User:**

> Thanks, that's great. Can you write u a PRD and ARDs for the result of our disuisaon. Include mermaid.js diagrams as appropriate. Name and explain the patterns in use. As well as the implementation and documentation of types and how they fit together including the marker interfaces. The session should be in the same transactional scope.
>
> Provide any other relevant information from a ln architectural standpoint and the anticipated shapes. We should have a definition for each type.

**Claude:**

*(Produced two files: `PRD-mediator-result-pipeline.md` and `ADR-mediator-result-pipeline.md`.)*

Two files: the **PRD** (goals, type catalog, mermaid diagrams for request flow / type composition / transactional scope, API mapping, anticipated future shapes) and the **ADR set** (8 records covering unions-as-Result, validation-vs-rule-violation split, template-method resource authorization, onion pipeline behaviors, exceptions-only-for-bugs, single transactional scope via scoped DI session, cancellation-token defaulting, marker interfaces).

The transactional scope point (ADR-0006) is the one worth double-checking against the actual data layer - it assumes a `Scoped`-lifetime session shared by `TransactionBehavior` and `ResourceCommandHandler` via constructor injection, which holds for standard per-HTTP-request DI but needs a manual scope for background/queue dispatch.

*(Patterns named in the PRD: CQRS, Mediator, Result/Railway-Oriented Programming via C# 15 unions, Chain of Responsibility / Onion middleware, Template Method, Marker Interface, Unit of Work.)*

---

## 5. Non-database requests, collections/paging, validator DI

**User:**

> How would we handle queries and commands that do not interact with the database where we want to avoid database sessions (assume other data stores or calling an API). Is this a case for a marker interface? How would it be composed?
>
> How do we handle an enumerable of results of T (paging / collection)?
>
> How do we handle dependency injection for what acts on T or a enumerable of T for example using the correct validation from Fluent Validation?

**Claude:**

### 1. Non-database queries/commands (external APIs, other stores)

Yes - existing marker interface territory, with a tightened subtlety: **absence of `ITransactionalRequest` should mean the session is never even constructed**, not just "not wrapped in a transaction."

Resolve the session lazily:

```csharp
services.AddScoped(sp => new Lazy<AppDbContext>(sp.GetRequiredService<AppDbContext>));

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly Lazy<AppDbContext> _db;
    public TransactionBehavior(Lazy<AppDbContext> db) => _db = db;

    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (request is not ITransactionalRequest)
            return await next(); // _db.Value is never evaluated - no session created

        var db = _db.Value;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = await next();
        if (result.Value is not Failure) { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        else await tx.RollbackAsync(ct);
        return result;
    }
}
```

Make the "intentionally no DB" decision explicit with a paired marker, rather than relying on absence:

```csharp
public interface ITransactionalRequest { }   // opts into the DB unit-of-work
public interface IExternalStoreRequest { }   // documents "deliberately no DB session"
```

An architecture test asserts every `ICommand` implements exactly one. Handlers for `IExternalStoreRequest` commands inject whatever client they need (e.g. `ICrmClient`), same as `ResourceCommandHandler.LoadAsync` already allows. Retries/circuit breakers for the external call remain a Polly/client-layer concern, not a pipeline behavior - genuine transient faults still throw.

### 2. Enumerable / paged results

Don't return `IEnumerable<T>`/`IAsyncEnumerable<T>` as the success payload - risks deferred execution outliving a scoped session, and can't carry paging metadata.

```csharp
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page * PageSize < TotalCount;
}
```

Used as `Result<PagedResult<WidgetDto>>` - no change to `Failure` or any behavior, since `PagedResult<T>` is just another success payload shape flowing through the same generic `Result<T>` contract. Empty result set is success, not `NotFound`.

### 3. DI for validators over `T` (and collections of `T`)

Falls out of existing generic composition - `ValidationBehavior<TRequest,TResponse>` never looks at `TResponse`, only `TRequest`:

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (!_validators.Any()) return await next();
        var errors = new Dictionary<string, List<(string Code, string Message)>>();
        foreach (var v in _validators)
            foreach (var e in (await v.ValidateAsync(request, ct)).Errors)
                (errors.TryGetValue(e.PropertyName, out var l) ? l : errors[e.PropertyName] = []).Add((e.ErrorCode, e.ErrorMessage));
        return errors.Count > 0
            ? new ValidationFailed(errors.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<(string, string)>)kv.Value))
            : await next();
    }
}
```

Registration: `AddValidatorsFromAssemblyContaining<...>()` + `AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))`. For a command containing a collection, compose per-item validation via `RuleForEach`, injecting the item validator:

```csharp
public sealed class BulkCreateWidgetsCommandValidator : AbstractValidator<BulkCreateWidgetsCommand>
{
    public BulkCreateWidgetsCommandValidator(IValidator<CreateWidgetItem> itemValidator)
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(itemValidator);
    }
}
```

Behaviors never need to know whether `TResponse` is `T`, `IReadOnlyList<T>`, or `PagedResult<T>` - that's the payoff of keeping `TResponse` opaque to every behavior.

---

## 6. Addendum to PRD/ADR

**User:**

> Great. Make addendum to the PRD / ARDs based on these findings and decisions. Include types, and meaningful logical examples, and any other helpful reference points. Assume this supplomental / continuation of what was already written assuming these changes haven't negated any of the previous statements

**Claude:**

*(Appended "Addendum A" to the PRD - non-database requests, paged results, validator composition, updated type catalog, a session-resolution mermaid diagram - and "ADR-0009 through ADR-0011" to the ADR document, covering the same three decisions in ADR format.)*

Both files updated in place with an **Addendum** section each, so the originals stay intact and these read as continuations:

- **PRD** - Addendum A covers non-DB requests (lazy `Lazy<AppDbContext>` + paired `ITransactionalRequest`/`IExternalStoreRequest` markers), collection/paged results (`PagedResult<T>`), and validator composition (scalar, bulk via `RuleForEach`, paged queries) - each with a worked example, an updated type-catalog table, and a new mermaid decision-flow diagram for session resolution.
- **ADRs** - three new records (ADR-0009, 0010, 0011) documenting each decision with context/consequences/alternatives, explicitly noting they extend rather than revise the earlier set.

---

## 7. Worked examples with XML documentation

**User:**

> Ok, now we're ready for some meaningful examples in action. Create some examples in use with comments and descriptions and XML documentation

**Claude:**

*(Created a set of example source files: `Core/Results.cs`, `Core/Abstractions.cs`, `Core/PipelineBehaviors.cs`, `Core/ResultExtensions.cs`, `Widgets/WidgetEntities.cs`, `Widgets/CreateWidgetCommand.cs` (simple transactional command), `Widgets/UpdateWidgetCommand.cs` (`ResourceCommandHandler` example with ownership authorization and concurrency conflict), `Widgets/BulkCreateWidgetsCommand.cs` (collection validation), `Widgets/GetWidgetsQuery.cs` (paged, non-transactional query), `Crm/SyncCustomerToCrmCommand.cs` (external-store command), and `Api/WidgetsController.cs` - each fully XML-documented, tying every design decision back to the earlier discussion. No closing narrative message was sent for this turn; the files themselves were the deliverable.)*

---

## 8. Library extraction - packaging, naming, and new query/command variants

**User:**

> How would this implementation be enriched and documented so that it could be used in a reusable library how should it be split up (single shared kernel?) What should the library be called, and what is "defining" about it that other libraries and packages don't do?
>
> Create the variants for commands and queries. <T> , enumerable, and paged as well as an example pipeline of usage including nesting doll expression, marker interface and example as well as targeted logic down with do as well as anything else that may be reasonable

**Claude:**

*(Created `Addendum-B-library-packaging.md` plus a package-structured code set: `Interlocutor/Results.cs`, `Interlocutor.Mediation/Abstractions.cs`, `Interlocutor.Mediation/Mediator.cs`, `Interlocutor.Mediation/GenericBehaviors.cs`, `Interlocutor.Mediation.EntityFrameworkCore/TransactionBehavior.cs`, `Interlocutor.Mediation.FluentValidation/ValidationBehaviors.cs`, `Interlocutor.AspNetCore/ResultExtensions.cs`, `Example/CompositionAndUsage.cs`.)*

Summary of what was delivered:

**Packaging** - a real shared kernel is only the dependency-free `Result`/`Failure` vocabulary; everything else (mediation abstractions, EF/FluentValidation/ASP.NET bindings) is a satellite. Proposed name **`Interlocutor`** - the legal/procedural term for how a case was resolved, unclaimed on NuGet, doesn't collide with `Task.Result`. What's defining vs. `OneOf`/`FluentResults`/`ErrorOr`/`MediatR`: native-union exhaustiveness (no source generator), the Result vocabulary and pipeline co-designed rather than bolted together, `ValidationFailed`/`RuleViolation` kept semantically distinct, resource-based authorization as a named first-class pattern, and a hard no-exceptions-as-Failure rule enforced in the library's own code, not just docs.

**New query/command variants**: `IQuery<T>`, `IEnumerableQuery<T>`, `IPagedQuery<T>` (load-bearing `Page`/`PageSize`), `ICommand<T>`, `ICommand`, `IEnumerableCommand<T>`, `IBulkCommand<TItem,TResponseItem>` (load-bearing `Items`) - the last two make cross-cutting validation reusable across *any* app without per-request boilerplate: `PagingValidationBehavior` and `BulkItemValidationBehavior` work off the interface, not a naming convention.

**The nesting-doll made concrete** (in `Mediator.cs`) - the actual fold that builds `Logging(next: Auditing(next: Validation(...(next: Handler))))` from a registered behavior list, with the equivalent hand-written version documented alongside it.

**`Example/CompositionAndUsage.cs`** ties it together: a paged query and a bulk command using the new interfaces, DI registration order, and a fully narrated trace of one request - including a short-circuit at item validation and the happy path all the way down to the returned `WidgetDto`.

---

## 9. Service locator usage - guidance and type safety

**User:**

> I see the usage of a service locator service. Is there any guidance around that, or opportunities to make that type safe. How and when is it used?
>
> Would would I request resources (validation, logging, ect.) for type T, or is this used for something else?

**Claude:**

`Mediator.Dispatch` was doing real service-locator-style resolution and calling through `dynamic`, which is worth separating into two questions.

### The guidance

Mark Seemann's "Service Locator is an anti-pattern" critique is specific: it's a problem when **application/business code** reaches into a container to fetch its own dependencies, hiding them from the constructor signature and defeating compile-time checking. It is **not** the same problem when confined to a small piece of generic infrastructure whose entire job is runtime dispatch on a type only known at the call site - MediatR, ASP.NET's controller activation, and AutoMapper's internal resolution are independent examples of the same general confinement pattern. The rule of thumb: **confine it to one class, and make sure nothing downstream (handlers, behaviors) ever touches `IServiceProvider` directly** - already true here, since every behavior/handler uses ordinary constructor injection.

### Where it's used, and why it can't be fully avoided

Exactly two lookups, both inside `Dispatch`: resolving the concrete handler for the request's runtime type, and resolving the ordered `IPipelineBehavior<TRequest,TResponse>` list for that same type. `Send<TResponse>(IQuery<TResponse> query, ...)` only knows the *static* type is `IQuery<TResponse>` - there's no way to write `services.GetRequiredService<IQueryHandler<???, TResponse>>()` without a runtime type token.

### What was avoidably unsafe: `dynamic`

Fixed with a small **handler-wrapper hierarchy** - a non-generic `HandlerWrapper<TResponse>` base, with fully statically-typed generic implementations (`QueryHandlerWrapper<TQuery,TResponse>`, `CommandHandlerWrapper<TCommand,TResponse>`) constructed once via reflection and cached in a `ConcurrentDictionary<Type,object>`. Every call after construction is ordinary, compiler-checked C# - no `dynamic`, no runtime binder. (MediatR uses a similar technique internally.)

Added `InterlocutorStartupValidation.ValidateInterlocutorHandlerRegistrations(...)` to close the remaining gap: a missing handler registration would otherwise only surface on the first production request that hits it; this scans all `IQuery<T>`/`ICommand<T>`/`ICommand` types at boot and fails startup instead.

### Would you use it to fetch validation/logging for `T`? No.

`ValidationBehavior<TRequest,TResponse>(IEnumerable<IValidator<TRequest>> validators)`, `LoggingBehavior<TRequest,TResponse>(ILogger<...> logger)`, `AuthorizationBehavior<TRequest,TResponse>(IPolicyEvaluator policyEvaluator)` - all ordinary constructor injection. The container resolves those dependencies automatically the moment it constructs the behavior instance, which happens *inside* `Mediator`'s one locator call - not because the behavior calls `GetService` itself. If a handler or behavior ever calls `IServiceProvider.GetService<T>()` directly from its own logic, that's the anti-pattern reappearing; the fix is always to add it as a constructor parameter instead.

---

## 10. Two worked examples: query and command

**User:**

> Ok, so I do set the logic for a given query and command. Provide two examples

**Claude:**

*(Created `Example/OrderExamples.cs`.)*

Two examples, deliberately annotated so it's obvious which lines are yours versus the library's:

**Query - `GetOrderByIdQuery`**: you write the record shape, the `AbstractValidator` rules, and the handler body (what counts as `NotFound`, what projection to return). The library only guarantees your validator runs before your handler, and that your handler's `Result<OrderDto>` flows back through the pipeline unchanged.

**Command - `CancelOrderCommand`**: same split, but through `ResourceCommandHandler` - you supply `LoadAsync` (your query), `Authorize` (your ownership rule), and `HandleAsync` (your business rule - note `RuleViolation` here, not `ValidationFailed`, since "already shipped" depends on loaded state, not input shape). The library's contribution is calling those three methods in order and committing the transaction once you return.

The registration block is the only place the two connect to the pipeline - and it's the same two lines for every new query or command added. Nothing about adding either example required touching `LoggingBehavior`, `TransactionBehavior`, or any other pipeline registration - concrete proof that the cross-cutting plumbing and business logic stay fully decoupled.

---

## 11. A generic Ingress/Egress pipeline stage

**User:**

> Provide an appropriate genetic type that provides the Ingress or Egress actions/functions not of which that are appropriately called but may be a no-op

**Claude:**

*(Created `PipelineStage.cs`, and refactored `LoggingBehavior`/`AuditingBehavior` to use it.)*

**`PipelineStage<TRequest,TResponse>`** - implements `IPipelineBehavior<TRequest,TResponse>` explicitly (so a subclass can't reopen/short-circuit it), and exposes two `protected virtual` hooks:

- `OnIngress(request, ct)` - runs before `next()`
- `OnEgress(request, result, ct)` - runs after `next()`, given the result

Both default to `Task.CompletedTask` and are **always invoked**, regardless of whether a subclass overrides them - the "appropriately called... may be a no-op" contract: the framework's obligation to call both sides is unconditional; whether a given stage does anything with the call is the subclass's choice.

`AuditingBehavior` is the clean demonstration - it only overrides `OnEgress` (it needs the outcome to decide what to record), so `OnIngress` runs as an inherited no-op every dispatch, invisibly. `LoggingBehavior` uses both.

Deliberate limitation, called out in the docs: this type can't express **short-circuiting** (`OnIngress` can't refuse to call `next()`) or **result-dependent branching that changes what happens** (`OnEgress` can observe but not replace the result). `ValidationBehavior`, `AuthorizationBehavior`, and `TransactionBehavior` stay as direct `IPipelineBehavior` implementations because they need to either reject before `next()` or make a commit/rollback decision *because of* the result - control flow, not observation. `PipelineStage<TRequest,TResponse>` is scoped to the purely observational subset.

---

Also caught and fixed a real defect while compiling this: `ResourceCommandHandler<TCommand,TEntity,TResponse>` never actually declared `: ICommandHandler<TCommand,TResponse>` - so the DI registrations in the Widget/Order examples (`AddScoped<ICommandHandler<CancelOrderCommand, OrderDto>, CancelOrderCommandHandler>()`) wouldn't have compiled. Fixed.

## Interfaces that exist

| Interface | Purpose |
|---|---|
| `IQuery<TResponse>` | Marks a read request returning `TResponse`. |
| `IEnumerableQuery<TItem>` | Readability specialization of `IQuery<IReadOnlyList<TItem>>`; no behavior keyed off it specifically. |
| `IPagedQuery<TItem>` | Paged-read contract exposing `Page`/`PageSize` as real members, so `PagingValidationBehavior` can validate any implementer generically. |
| `ICommand<TResponse>` | Marks a write request returning `TResponse`. |
| `ICommand` | Marks a write request with no return payload. |
| `IEnumerableCommand<TResponseItem>` | Readability specialization of `ICommand<IReadOnlyList<TResponseItem>>`. |
| `IBulkCommand<TItem,TResponseItem>` | Bulk-write contract exposing `Items`, so `BulkItemValidationBehavior` can validate every item generically. |
| `ITransactionalRequest` | Opts a command into the SQL unit-of-work (`TransactionBehavior` resolves the session). |
| `IExternalStoreRequest` | Declares a command deliberately has no SQL session - paired with the above. |
| `IAuditableRequest` | Opts a request into audit logging; exposes `ActionName`. |
| `IRequiresPolicyRequest` | Opts a request into a named, non-resource authorization check; exposes `PolicyName`. |
| `IQueryHandler<TQuery,TResponse>` | Contract for a query's handler. |
| `ICommandHandler<TCommand,TResponse>` | Contract for a value-returning command's handler. |
| `ICommandHandler<TCommand>` | Contract for a no-payload command's handler. |
| `IPipelineBehavior<TRequest,TResponse>` | Contract for one cross-cutting concern wrapping the handler (§3, §11 of the export). |
| `IMediator` | The single dispatch entry point (`Send` overloads). |
| `IAuditSink` | Infrastructure seam `AuditingBehavior` writes through; app supplies the implementation. |
| `IPolicyEvaluator` | Infrastructure seam `AuthorizationBehavior` checks through; app supplies the implementation. |

## Implementations that exist

| Class | Implements | Purpose |
|---|---|---|
| `Mediator` | `IMediator` | Resolves the concrete handler/behavior chain for a request's runtime type via cached, statically-typed wrappers (no `dynamic`); folds behaviors into the nested pipeline. |
| `ResourceCommandHandler<TCommand,TEntity,TResponse>` | `ICommandHandler<TCommand,TResponse>` *(just fixed)* | Template method: load → resource-authorize → handle, without threading state through pipeline behaviors. |
| `PipelineStage<TRequest,TResponse>` | `IPipelineBehavior<TRequest,TResponse>` | Abstract base offering always-called, default-no-op `OnIngress`/`OnEgress` hooks for purely observational behaviors. |
| `LoggingBehavior<TRequest,TResponse>` | `PipelineStage<...>` | Logs entry/duration/outcome for every request. |
| `AuditingBehavior<TRequest,TResponse>` | `PipelineStage<...>` | Records an audit entry for `IAuditableRequest` requests via `IAuditSink`. |
| `AuthorizationBehavior<TRequest,TResponse>` | `IPipelineBehavior<...>` | Evaluates `IRequiresPolicyRequest` requests via `IPolicyEvaluator`; can short-circuit, so it can't use `PipelineStage`. |
| `PagingValidationBehavior<TRequest,TItem>` | `IPipelineBehavior<...>` | Validates `Page`/`PageSize` bounds for any `IPagedQuery<TItem>`. |
| `TransactionBehavior<TRequest,TResponse,TContext>` | `IPipelineBehavior<...>` | Lazily opens/commits/rolls back a SQL transaction for `ITransactionalRequest` commands. |
| `ValidationBehavior<TRequest,TResponse>` | `IPipelineBehavior<...>` | Runs registered FluentValidation `IValidator<TRequest>`s, short-circuits with `ValidationFailed`. |
| `BulkItemValidationBehavior<TRequest,TItem,TResponseItem>` | `IPipelineBehavior<...>` | Validates every item of an `IBulkCommand<,>` via `IValidator<TItem>`. |
| `ResultExtensions` (static) | - | The single `Result`/`Failure` → HTTP mapping. |

## What's missing for a "true" mediator/CQRS implementation

| Should exist | Purpose |
|---|---|
| **`INotification` + `INotificationHandler<TNotification>` + `IMediator.Publish(...)`** | The other half of the mediator pattern. `Send` is strictly one-handler request/response; a real mediator also supports **publish**: broadcasting a domain/integration event (e.g. `OrderCancelled`) to zero-or-more independent handlers, with no `Result<T>` contract and no coupling between the command handler and whatever reacts to it. This is the biggest structural gap - nothing here currently lets a command handler raise a fact for others to react to. |
| **`IPipelineBehavior<TRequest>` (no `TResponse`) + a matching wrapper** | Real gap, not hypothetical: `Mediator.Send(ICommand command)` (the no-payload path) currently bypasses the entire behavior pipeline - no logging, validation, or transaction runs for it, because it was left as a simplified reflection call. No-payload commands need the same wrapper treatment as `CommandHandlerWrapper<TCommand,TResponse>`, just returning `Result` instead of `Result<TResponse>`. |
| **`IStreamQuery<TItem>` / `IStreamQueryHandler<TQuery,TItem>` returning `IAsyncEnumerable<T>`** | Flagged as future work in the PRD (Addendum A.2) but never built. Needed for genuinely large exports where materializing a full `PagedResult<T>` isn't viable - a deliberately separate contract from `Result<T>`, since streaming and "commit/rollback as one unit" don't compose. |
| **`ISender` / `IPublisher` (splitting `IMediator`)** | Interface Segregation: most consumers only ever call `Send`; splitting lets a handler that only needs to publish events depend on `IPublisher` without pulling in the ability to dispatch arbitrary commands, and vice versa. |
| **An assembly-scanning registration extension (`AddInterlocutorMediation(params Assembly[])`)** | Every handler and validator is currently registered by hand, one line per type. A "true" reusable library ships a scanner that finds every `IQueryHandler<,>`/`ICommandHandler<,>`/`IValidator<>` in given assemblies and registers them automatically - this is what makes `Interlocutor` competitive with MediatR's `AddMediatR(...)` ergonomically, and it should call `InterlocutorStartupValidation` automatically at the end. |
| **`IRequestExceptionHandler<TRequest,TResponse,TException>`** | Distinct from ADR-0005's "exceptions are exceptional" stance - this isn't about converting exceptions into `Failure`. It's about letting a request type declare typed enrichment/logging/translation for a *specific* infrastructure exception (e.g. wrap `SqlException` with request context before it propagates) without ever turning it into a domain outcome. Optional, but a mature mediator library usually offers it as an escape hatch. |
| **`IRequestContextAccessor` (correlation id / current user / tenant)** | Right now `ICurrentUser` is ad hoc, defined per example rather than by the library. A shared ambient-context abstraction would let `LoggingBehavior`/`AuditingBehavior`/tracing pull a correlation id consistently instead of each app inventing its own. |

The `Publish`/notification gap and the no-payload pipeline gap are the two I'd treat as priorities - the first is a missing half of "mediator" by definition, the second is a latent bug in what's already shipped.
