# Messages, Result Types & Variance

Request types (queries and commands) and their response type constraints. Result types are C# 15 discriminated unions, and every response is constrained to implement `System.Runtime.CompilerServices.IUnion` - a bare DTO response type fails to compile.

## Request Types (Messages)

Requests are marked with a carrier interface:

### IQuery<TResponse>

Marks a read request. Never writes data. Always returns `TResponse : IUnion`.

```csharp
public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;

public record GetSetting(string Key) : IQuery<Outcome<string, Failure>>;
```

Dispatch:

```csharp
var result = await mediator.Send(new GetUser(42));  // Outcome<UserDto, NotFound>
```

### ICommand<TResponse>

Marks a write request that returns a value. Returns `TResponse : IUnion`.

```csharp
public record CreateUser(string Name, string Email)
    : ICommand<Outcome<int, ValidationError>>;  // Returns new user ID

public record TransferFunds(int FromAccountId, int ToAccountId, decimal Amount)
    : ICommand<Outcome<Success, InsufficientFunds, Conflict>>;
```

Dispatch:

```csharp
var result = await mediator.Send(new CreateUser("Alice", "alice@example.com"));
// result is Outcome<int, ValidationError> with new user ID or validation errors
```

### ICommand (Unit Command)

Marks a write request with no domain data payload - only an outcome. Unlike `ICommand<TResponse>`, the outcome type isn't declared on the command itself - it's chosen **at the call site** via `Send<TResult>`'s type argument, so the same command can be handled differently depending on what outcome information a given caller needs:

```csharp
public record DeleteAllUsers() : ICommand;

public record ClearCache() : ICommand;
```

A handler is `IVoidCommandHandler<TCommand, TResult>`, pairing the command with whichever `IUnion`-shaped result that handler produces - any union is legal, including the case-less, non-generic `Outcome` for an operation that is genuinely infallible:

```csharp
public class ClearCacheHandler : IVoidCommandHandler<ClearCache, Outcome>
{
    public async Task<Outcome> Handle(ClearCache command, CancellationToken ct)
    {
        await this.cache.ClearAsync(ct);
        return new Outcome();
    }
}
```

Dispatch:

```csharp
var result = await mediator.Send<Outcome>(new ClearCache());
// result confirms ClearCache ran - there is nothing further to inspect (see Result Types § Outcome below)
```

## IUnion Constraint

Every handler's `TResponse` must implement `System.Runtime.CompilerServices.IUnion` - the interface the compiler generates for every `union` declaration:

```csharp
public interface IQuery<TResponse>
    where TResponse : IUnion
{
}

public interface ICommand<TResponse>
    where TResponse : IUnion
{
}
```

This constraint is **compiler-enforced**. A bare DTO response type fails to compile:

```csharp
// ❌ Compile error - UserDto doesn't implement IUnion
public record GetUser(int UserId) : IQuery<UserDto>;

// ✅ Correct
public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;
```

The constraint enables runtime-typed dispatch (see [runtime-typed-dispatch.md](runtime-typed-dispatch.md)) and ensures callers can pattern-match on success/failure without knowing the concrete outcome shape.

## IUnion as an Extension Point

`IUnion` is the real extension surface, not an implementation detail of the built-in unions. The compiler-generated `Outcome<...>` unions documented throughout this page are the library's own built-in implementation of that interface - not the only legal one. Any type that implements `IUnion` can stand in as a handler's response, including a hand-rolled class that isn't a compiler-generated `union` at all:

```csharp
public sealed class CustomOutcome : IUnion
{
    private CustomOutcome(Type caseType, object? caseValue)
    {
        this.CaseType = caseType;
        this.CaseValue = caseValue;
    }

    public Type CaseType { get; }

    public object? CaseValue { get; }

    object? IUnion.Value => this.CaseValue;

    public static CustomOutcome FromSuccess(Success value) => new(typeof(Success), value);

    public static CustomOutcome FromFailure<TFailure>(TFailure failure)
        where TFailure : notnull => new(typeof(TFailure), failure);
}
```

A hand-rolled `IUnion` implementer needs nothing beyond that one interface to be a legal `TResponse` - it does **not** need to implement `IOutcome`, the project-owned marker the shipped `Outcome`/`Outcome<...>` family also declares. `IOutcome` and its companion `IOutcomeCase` still exist in `Interlocutor.CommonKernel.ResultTypes`, but they're a separate, opt-in classification surface now, not what makes a type legal as `TResponse`: the shipped family declares `: IOutcome` to expose it, and your own hand-rolled type may declare `IOutcomeCase` too if it wants the same surface - see [Glossary § Result Types](glossary.md#result-types) for what that surface actually gives you.

What makes a response type usable by the *pipeline*, though, isn't a `union` declaration either - it's being teachable to `IOutcomeCaseRegistry`, the construction/extraction seam behaviors use to build and inspect a response's active case without knowing its concrete shape. `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, and the FluentValidation `ValidationBehavior` all go through this registry rather than constructing a case directly, which is what lets them work against both compiler-generated unions and a consumer's own `IUnion` implementation.

`DefaultOutcomeCaseRegistry` is the zero-config default, and it splits into two halves that aren't equally permissive. *Extracting* a response's active case works for any `IUnion` implementer generically - it just reads `Value` - so `CustomOutcome` above needs no custom extractor registered at all. *Constructing* one from a case value is narrower: the default registry finds a case constructor only for a compiler-generated `union`, so a hand-rolled type like `CustomOutcome`, which has no case constructor to find, still needs a custom factory registered via `OutcomeCaseRegistryBuilder`:

```csharp
var registry = new OutcomeCaseRegistryBuilder()
    .RegisterFactory<CustomOutcome, Forbidden>(CustomOutcome.FromFailure)
    .Build();
```

Anything left unregistered still falls back to `DefaultOutcomeCaseRegistry`, so a consumer only needs to register construction for the response types that aren't unions - every `Outcome<...>` shape keeps working unmodified. Register a custom registry via `InterlocutorPipelineBuilder.OverrideOutcomeCaseRegistry<TRegistry>()` (reachable via `builder.Pipeline`); see [pipeline-and-behaviors.md](pipeline-and-behaviors.md) for how the behaviors that depend on it are wired.

## Result Types

`Interlocutor.CommonKernel` ships the standard failure case types and outcome unions - these are the standard shapes. See [Composing Outcome Arities](composing-outcome-arities.md) for worked examples of picking between them for a given command or query.

> [!NOTE]
> **Naming convention.** The case types below follow one deliberate pattern:
>
> - **Singular default** (`Conflict`, `NotFound`, `Failure`, `ValidationError`, ...) - one occurrence of that failure.
> - **Plural aggregate** (`Conflicts`, `Failures`, `ValidationErrors`, ...) - a collection of one or more occurrences of the same singular case, for a handler that can fail for more than one independent reason in a single response. Reach for the plural aggregate instead of collecting your own `List<T>` of the singular: it de-duplicates, is safe at its default value, and exposes the same `.Items`-plus-semantic-projection shape (`.Reasons`, `.Messages`, `.ErrorCodes`, ...) across every aggregate, so callers pattern-match one familiar shape regardless of which aggregate they're holding.
> - **`I<Concept>FailureCase` marker** (`IForbiddenFailureCase`, `IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IValidationFailureCase`, `IFieldValidationFailureCase`) - a minimal interface the matching built-in case type implements, naming just enough shape (a `Message`, or `Messages`) for a consumer to recognize *any* implementer of that concept generically. Implementing one on your own case type buys substitutability: `OutcomeMapperBase` (and anything built on it, like `DefaultHttpOutcomeStatusMapper`) already recognizes the marker rather than the concrete built-in type, so a domain-specific replacement - a richer `Forbidden`, a technology-specific `RateLimited` - maps correctly with zero mapper changes. This mirrors the [`IUnion`](#iunion-as-an-extension-point) extension-point shape one level down: `IUnion` lets you substitute the whole union, an `I<Concept>FailureCase` marker lets you substitute one of its cases.

### Success

Marker type for an operation that succeeds with no payload - one ordinary case among an arity's others, for an operation that genuinely can fail (an infallible operation uses the case-less, non-generic `Outcome` instead - see [ICommand (Unit Command)](#icommand-unit-command) above):

```csharp
public readonly record struct Success;

public record ArchiveOldOrders() : ICommand<Outcome<Success, Failure>>;

public class ArchiveOldOrdersHandler : ICommandHandler<ArchiveOldOrders, Outcome<Success, Failure>>
{
    public async Task<Outcome<Success, Failure>> Handle(ArchiveOldOrders command, CancellationToken ct)
    {
        var archived = await this.orders.ArchiveOlderThanAsync(TimeSpan.FromDays(90), ct);
        return archived > 0 ? new Success() : new Failure("No orders were old enough to archive.");
    }
}
```

### NotFound

Entity does not exist:

```csharp
public readonly record struct NotFound(string? Message = null);

public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;

var result = await mediator.Send(new GetUser(999));
var notFound = Assert.IsType<NotFound>(result.Value);
Assert.NotNull(notFound.Message);
```

### Forbidden

Authorization denied for a resource:

```csharp
public readonly record struct Forbidden(string? Reason = null);

public record DeleteDocument(int DocumentId)
    : ICommand<Outcome<Success, NotFound, Forbidden>>;

var result = await mediator.Send(new DeleteDocument(42));
var forbidden = Assert.IsType<Forbidden>(result.Value);
```

### ValidationError

Input validation failure. Produced by `ValidationBehavior` when FluentValidation rules fail:

```csharp
public readonly record struct ValidationError(string Message);

// With a machine-readable error code
public readonly record struct ValidationError<TErrorCode>(string Message, TErrorCode ErrorCode)
    where TErrorCode : notnull, IEquatable<TErrorCode>;

// Aggregated - construct from messages, from ValidationError instances, or from a single message
var errors = new ValidationErrors(["Email is required", "Password too short"]);

var result = await mediator.Send(new CreateUser("", "bad-email"));
// result.Value is a ValidationErrors whose .Items/.Messages expose the collected errors
```

### Conflict

State conflict - concurrency, uniqueness violation, invalid transition. Both `Message` and `Code` are mandatory - there is no bare form:

```csharp
public readonly record struct Conflict(string Message, string Code);

public record UpdateProduct(int ProductId, string Name, int Version)
    : ICommand<Outcome<Success, NotFound, Conflict>>;

var result = await mediator.Send(new UpdateProduct(1, "New Name", 3));
// If another request updated the product (version mismatch), result is Conflict
```

### Failure

Generic fallback failure when none of the specific cases apply. `Failure` is reason-only; `Failure<TErrorCode>` adds a machine-readable error code, with `TErrorCode` fixed for the lifetime of the value:

```csharp
public readonly record struct Failure(string? Reason = null);

public readonly record struct Failure<TErrorCode>(TErrorCode ErrorCode, string? Reason = null)
    where TErrorCode : notnull, IEquatable<TErrorCode>;

public record GetSetting(string Key) : IQuery<Outcome<string, Failure>>;

var result = await mediator.Send(new GetSetting("unknown.key"));
// result.Value is Failure("Setting not found")
```

Aggregate multiple failures with `Failures` (or `Failures<TErrorCode>` for the error-code variant) when a handler can fail for more than one independent reason at once:

```csharp
public record BulkDelete(IReadOnlyList<int> Ids) : ICommand<Outcome<Success, Failures>>;

var result = await mediator.Send(new BulkDelete([1, 2, 3]));
// result.Value is Failures whose .Items holds one Failure per record that couldn't be deleted,
// and .Reasons projects the non-null reasons
```

### Cancelled, Timeout & RateLimited

Three additional case types cover cross-cutting concerns that opt-in pipeline behaviors (see [pipeline-and-behaviors.md](pipeline-and-behaviors.md)) can short-circuit with:

```csharp
public readonly record struct Cancelled(string? Reason = null);

public readonly record struct Timeout(TimeSpan Budget, TimeSpan Elapsed, string? Message = null);

public readonly record struct RateLimited(string LimitKey, TimeSpan? RetryAfter = null, string? Message = null);

public record ExportReport(int ReportId)
    : ICommand<Outcome<Success, Cancelled, Timeout, RateLimited>>, ICancellable, IHasTimeout, IRateLimited
{
    public TimeSpan Budget => TimeSpan.FromSeconds(30);

    public string LimitKey => "exports";
}
```

- `Cancelled` - the ambient `CancellationToken` was cancelled mid-flight and `CancellationBehavior` converted the resulting `OperationCanceledException` into an outcome instead of letting it propagate.
- `Timeout` - `TimeoutBehavior` raced the rest of the pipeline against a resolved time budget and the budget elapsed first; carries both the budget and the actual elapsed time for diagnostics.
- `RateLimited` - `RateLimitedBehavior` rejected the request because an `IRateLimiter` check disallowed it; `LimitKey` identifies which limiter/bucket was hit.

None of these three participate unless the request opts in via the matching marker interface (`ICancellable`, `IHasTimeout`, `IRateLimited`) and the corresponding behavior is registered - the same "nothing runs unless you ask for it" rule that governs every pipeline behavior.

## Outcome Unions

Every generic arity is a C# 15 discriminated union - except arity 0, which is deliberately not a union at all:

```csharp
// Arity 0: case-less acknowledgment - no cases, no payload, no success/failure judgment
public readonly record struct Outcome : IUnion;

// Generic union: success + specific failure cases
public readonly union Outcome<T1, T2>(T1, T2);

// Wider arities (2 to 6 cases)
public readonly union Outcome<T1, T2, T3>(
    T1, T2, T3);
```

The non-generic `Outcome` is a small, hand-written type - not generator-emitted, since it declares no case types at all. Holding an instance of it is itself the acknowledgment that an operation ran; it has no `Match`, `Switch`, or `TryGetValue` members, since there is nothing to dispatch over. Reserve it for a request that is genuinely infallible and opts into none of `ICancellable`/`IHasTimeout`/`IRateLimited` - see [Choosing Arities](#choosing-arities) below.

Every generic form from two cases through six carries the identical generated member set: `Match`, `Switch`, a per-case `TryGetValue`, `ToString`, and value equality (`Equals`, `GetHashCode`, `IEquatable<TSelf>`, `==`/`!=`). None of it is hand-written; each arity is declared as a `partial union` attributed with `GenerateOutcomeMembersAttribute`, and a source generator emits the second partial declaration - see [Pattern Matching](#pattern-matching) below for how to use the generated surface, [Value Equality](#value-equality) for how the generated equality members behave, and [Extensibility § Duplicate case type detection](extensibility.md#duplicate-case-type-detection-interloc009) for the companion analyzer that keeps every arity's case types distinct.

### Pattern Matching

Use `switch` expressions to handle each case:

```csharp
var result = await mediator.Send(new DeleteUser(42));
// result is Outcome<Success, NotFound, Forbidden>

var message = result.Value switch
{
    Success => "User deleted",
    NotFound => "User not found",
    Forbidden => "Access denied",
};
```

The compiler enforces that all cases are handled:

```csharp
// ❌ Compile error - Forbidden case not handled
var message = result.Value switch
{
    Success => "User deleted",
    NotFound => "User not found",
};

// ✅ Correct
var message = result.Value switch
{
    Success => "User deleted",
    NotFound => "User not found",
    Forbidden => "Access denied",
    _ => throw new InvalidOperationException(),  // Exhaustive fallback
};
```

A raw `switch` on `.Value` always works and needs nothing beyond the union itself, but every shipped arity also carries a generated `Match`/`Switch`/`TryGetValue` surface as an alternative, not a replacement:

```csharp
var query = new GetUser(42);
Outcome<UserDto, NotFound> result = await mediator.Send(query);

// Match projects to a value - one delegate per case, compiler-enforced exhaustive
var message = result.Match(
    user => $"Found: {user.Name}",
    notFound => notFound.Message ?? "User not found"
);

// Switch is Match's void-returning, side-effecting sibling
result.Switch(
    user => logger.LogInformation("Found {UserId}", user.Id),
    notFound => logger.LogWarning("Missing: {Message}", notFound.Message)
);

// TryGetValue reads one specific case without boxing a value-type case
if (result.TryGetValue(out UserDto user))
{
    return user;
}
```

`ToString()` is generated too, forwarding to the active case's own `ToString()` override - `result.ToString()` reads whatever the active case's own override produces (`NotFound`'s message, or a success DTO's own `ToString()`), rather than a default struct dump.

### Value Equality

`Equals`, `GetHashCode`, `IEquatable<TSelf>`, and `==`/`!=` are generated too, and every one of them delegates to the active case value's own equality (`this.Value.Equals(other.Value)`) rather than switching per case type:

```csharp
Outcome<UserDto, NotFound> left = new NotFound("missing");
Outcome<UserDto, NotFound> right = new NotFound("missing");

left == right;              // true - both hold an equal NotFound
left.Equals(right);         // true
left.GetHashCode() == right.GetHashCode(); // true
```

Two semantics are load-bearing here:

- **Union-type identity matters.** `Equals(object?)` checks `obj is TSelf` before delegating, so two different case types boxed into the same union never compare equal by coincidence, and a value boxed into one union arity never equals the "same" value boxed into a different arity - `Outcome<string, NotFound>` and `Outcome<string, NotFound, Forbidden>` are different runtime types even when both hold the success string `"hello"`.
- **Item order never matters for a collection-bearing case.** `Failures`, `Conflicts`, `ValidationErrors`, and their `<TErrorCode>`/FluentValidation counterparts already compare equal for the same distinct items regardless of the order those items were collected in - a result assembled from concurrent/async work that completes in a different order is still the same result. Because the union's own equality just delegates to `Value.Equals(other.Value)`, this order-independence carries straight through an `Outcome<T1, Failures>` (or any arity pairing one of these aggregate types) with no extra work:

```csharp
var failureA = new Failure("a");
var failureB = new Failure("b");

Outcome<Success, Failures> collectedAThenB = new Failures([failureA, failureB]);
Outcome<Success, Failures> collectedBThenA = new Failures([failureB, failureA]);

collectedAThenB == collectedBThenA; // true - same distinct items, different collection order
```

The case-less, non-generic `Outcome` carries a different, trivial equality: every instance equals every other, since there is no state to differ on.

## Choosing Arities

Before picking among failure cases, first ask whether the operation can fail at all. A genuinely infallible operation - one with no not-found/conflict/validation/business-rule case it could ever hit - is the case-less arity 0 (`Outcome`), not an `Outcome<Success, Failure>` whose `Failure` arm can never actually construct. Reach for a generic arity only once there's a real failure mode to report, and then don't route every failure through a single `Failure` - pick the arity that an operation can actually produce:

```csharp
// ✅ Specific - only NotFound and Failure are possible
public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;

// ❌ Over-broad - ValidationError should never occur (read-only query)
public record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound, ValidationError>>;

// ✅ Specific - a command can fail with input validation or a concurrency conflict
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<Outcome<Success, ValidationError, Conflict>>;

// ❌ Ambiguous - operation produces either a value or one of many failures, lose specificity
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<Outcome<Success, Failure>>;
```

When a handler's response arity is specific (only the cases it can produce), the compiler forces callers to handle exactly those cases - no more, no less.

## Marker Interfaces

Requests opt into cross-cutting pipeline behavior by implementing a [marker interface](glossary.md#marker-interfaces) - never by editing a central configuration list:

| Interface | Used by | Carries |
|-----------|---------|---------|
| `ITransactionalRequest` | `UnitOfWorkBehavior` (forces an eager transaction begin - every write already gets one automatically) | - |
| `IExternalStoreRequest` | Declares "no SQL session" | - |
| `IAuditableRequest` | `AuditingBehavior` | `ActionName` |
| `IRequiresPolicy` | `AuthorizationBehavior` | `PolicyName` |
| `ICancellable` | `CancellationBehavior` (see [Cancelled, Timeout & RateLimited](#cancelled-timeout--ratelimited) above) | - |
| `IHasTimeout` | `TimeoutBehavior` | `Budget` |
| `IRateLimited` | `RateLimitedBehavior` | `LimitKey` |

A request can implement multiple markers:

```csharp
public record TransferFunds(int FromAccountId, int ToAccountId, decimal Amount)
    : ICommand<Outcome<Success, ValidationError, InsufficientFunds>>,
      ITransactionalRequest,  // Opts into transaction wrapping
      IAuditableRequest,      // Opts into auditing
      IRequiresPolicy         // Opts into authorization
{
    public string ActionName => "transfer_funds";
    public string PolicyName => "TransferFunds";
}
```

See [pipeline-and-behaviors.md](pipeline-and-behaviors.md) for the full behavior/marker story.

## Variance: Response Type Is Never Variant

> [!CAUTION]
> **Response type variance is impossible and permanent.** This is the single most likely thing a future contributor will attempt and fail at.

The response type parameter appears in **both argument and return position** on a [pipeline behavior](glossary.md#pipeline)'s `IPipelineBehavior<TRequest, TResponse>` and lives inside a `Task` on handler interfaces:

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
    // TResponse is in return position (covariance)
    // AND inside Func<Task<TResponse>> - an argument position (contravariance)
}

public interface IQueryHandler<TQuery, TResponse> where TResponse : IUnion
{
    Task<TResponse> Handle(TQuery query, CancellationToken ct);
    // TResponse is in return position (covariance wanted here)
    // BUT lives in Task<TResponse>, which is covariant only in its success case,
    // not the delegate's parameter position
}
```

This dual usage prevents variance from being sound. You cannot declare `TResponse` as covariant or contravariant - it must be invariant.

**What this means in practice:**

```csharp
// ❌ Cannot do this - there's no implicit conversion from more-specific to less-specific response
public class GetUserHandler : IQueryHandler<GetUser, Outcome<Admin, NotFound>>
{
}

var handler = new GetUserHandler();  // Outcome<Admin, NotFound>
IQueryHandler<GetUser, Outcome<User, NotFound>> general = handler;  // ❌ Type mismatch

// ✅ Must be explicit (or use the exact same type in both places)
public class GetUserHandler : IQueryHandler<GetUser, Outcome<User, NotFound>>
{
}
```

Polymorphism is request-side only. Queries and commands can inherit from a base request type (contravariant on `TRequest`), but the response type is invariant across every interface in the library.

## Streaming: Deliberately Not Supported

There is no `IAsyncEnumerable<T>` / streaming response type - streaming and "commit/rollback as one unit" don't compose, so it's deliberately kept out permanently, not a gap waiting to be filled.

The constraint (all responses must be `IUnion`) ensures:

- A caller can inspect success/failure without knowing the concrete outcome shape (enables runtime-typed dispatch).
- Transactions can guarantee "all or nothing" - a single commit/rollback boundary.
- Pipeline behaviors can compose generically over `TResponse`.

Streaming violates both: a partial stream can fail halfway, the caller might not consume the stream, and the transaction boundary becomes unclear.

Use materialized collections instead:

```csharp
// ✅ Correct - materialized list, one outcome
public record GetUsers() : IQuery<Outcome<IReadOnlyList<UserDto>, Failure>>;

public class GetUsersHandler : IQueryHandler<GetUsers, Outcome<IReadOnlyList<UserDto>, Failure>>
{
    public async Task<Outcome<IReadOnlyList<UserDto>, Failure>> Handle(
        GetUsers query, CancellationToken ct)
    {
        var users = await this.repository.GetAllAsync(ct);
        return users.ToList().AsReadOnly();
    }
}

// ❌ Not supported - streaming response
public record GetUsers() : IQuery<IAsyncEnumerable<UserDto>>;
```

For paged results, define your own `PagedResult<T>` (a consumer-defined type carrying `Items`, `Page`, `PageSize`, `TotalCount` - Interlocutor doesn't ship one):

```csharp
public record GetUsers(int Page, int PageSize)
    : IQuery<Outcome<PagedResult<UserDto>, Failure>>;

var result = await mediator.Send(new GetUsers(1, 10));
// result.Value is a PagedResult with Items, Page, PageSize, TotalCount
```
