# Messages, Result Types & Variance

Request types (queries and commands) and their response type constraints. Result types are C# 15 discriminated unions, and every response is constrained to implement `IOperationOutcome` - a bare DTO response type fails to compile.

## Request Types (Messages)

Requests are marked with a carrier interface:

### IQuery<TResponse>

Marks a read request. Never writes data. Always returns `TResponse : IOperationOutcome`.

```csharp
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

public record GetSetting(string Key) : IQuery<OperationOutcome<string, Failure>>;
```

Dispatch:

```csharp
var result = await mediator.Send(new GetUser(42));  // OperationOutcome<UserDto, NotFound>
```

### ICommand<TResponse>

Marks a write request that returns a value. Returns `TResponse : IOperationOutcome`.

```csharp
public record CreateUser(string Name, string Email)
    : ICommand<OperationOutcome<int, ValidationError>>;  // Returns new user ID

public record TransferFunds(int FromAccountId, int ToAccountId, decimal Amount)
    : ICommand<OperationOutcome<Success, InsufficientFunds, Conflict>>;
```

Dispatch:

```csharp
var result = await mediator.Send(new CreateUser("Alice", "alice@example.com"));
// result is OperationOutcome<int, ValidationError> with new user ID or validation errors
```

### ICommand (Unit Command)

Marks a write request with no domain data payload - only success or failure. Returns `OperationOutcome` (the non-generic form, `Success | Failure`).

```csharp
public record DeleteAllUsers() : ICommand;

public record ClearCache() : ICommand;
```

Dispatch:

```csharp
var result = await mediator.Send(new DeleteAllUsers());
// result is OperationOutcome (Success or Failure)
```

## IOperationOutcome Constraint

Every handler's `TResponse` must implement `IOperationOutcome`:

```csharp
public interface IQuery<TResponse>
    where TResponse : IOperationOutcome
{
}

public interface ICommand<TResponse>
    where TResponse : IOperationOutcome
{
}
```

This constraint is **compiler-enforced**. A bare DTO response type fails to compile:

```csharp
// ❌ Compile error - UserDto doesn't implement IOperationOutcome
public record GetUser(int UserId) : IQuery<UserDto>;

// ✅ Correct
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;
```

The constraint enables runtime-typed dispatch (see [runtime-typed-dispatch.md](runtime-typed-dispatch.md)) and ensures callers can pattern-match on success/failure without knowing the concrete outcome shape.

## IOperationOutcome as an Extension Point

`IOperationOutcome` is the real extension surface, not an implementation detail of the built-in unions. The compiler-generated `OperationOutcome<...>` unions documented throughout this page are the library's own built-in implementation of that marker interface - not the only legal one. Any type that implements `IOperationOutcome` can stand in as a handler's response, including a hand-rolled class that isn't a compiler-generated `union` at all:

```csharp
public sealed class CustomOperationOutcome : IOperationOutcome
{
    private CustomOperationOutcome(Type caseType, object? caseValue)
    {
        this.CaseType = caseType;
        this.CaseValue = caseValue;
    }

    public Type CaseType { get; }

    public object? CaseValue { get; }

    public static CustomOperationOutcome FromSuccess(Success value) => new(typeof(Success), value);

    public static CustomOperationOutcome FromFailure<TFailure>(TFailure failure)
        where TFailure : notnull => new(typeof(TFailure), failure);
}
```

What makes a response type usable by the pipeline isn't a `union` declaration - it's being teachable to `IOutcomeCaseRegistry`, the construction/extraction seam behaviors use to build and inspect a response's active case without knowing its concrete shape. `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, and the FluentValidation `ValidationBehavior` all go through this registry rather than constructing a case directly, which is what lets them work against both compiler-generated unions and a consumer's own `IOperationOutcome` implementation.

`DefaultOutcomeCaseRegistry` is the zero-config default: it handles any `IOperationOutcome` that is a compiler-generated `union`, via reflection over the union's case constructors and its compiler-generated `IUnion` marker. A response type that isn't a `union` - like `CustomOperationOutcome` above - has no case constructor for the default registry to find, so it needs a custom factory/extractor pair registered via `OutcomeCaseRegistryBuilder`:

```csharp
var registry = new OutcomeCaseRegistryBuilder()
    .RegisterFactory<CustomOperationOutcome, Forbidden>(CustomOperationOutcome.FromFailure)
    .RegisterExtractor<CustomOperationOutcome>(
        (CustomOperationOutcome response, out Type caseType, out object? caseValue) =>
        {
            caseType = response.CaseType;
            caseValue = response.CaseValue;
            return true;
        }
    )
    .Build();
```

Anything left unregistered still falls back to `DefaultOutcomeCaseRegistry`, so a consumer only needs to register the response types that aren't unions - every `OperationOutcome<...>` shape keeps working unmodified. Register a custom registry via `InterlocutorPipelineBuilder.OverrideOutcomeCaseRegistry<TRegistry>()` (reachable via `builder.Pipeline`); see [pipeline-and-behaviors.md](pipeline-and-behaviors.md) for how the behaviors that depend on it are wired.

## Result Types

Interlocutor.CommonKernel ships the standard failure case types and outcome unions. The exploration guide at `docs/guides/composing-operationoutcome-arities.md` walks through concrete examples; these are the standard shapes:

### Success

Marker record for unit command success - carries no payload:

```csharp
public record Success;

// Use in unit commands
public record ClearCache() : ICommand;

public class ClearCacheHandler : ICommandHandler<ClearCache>
{
    public async Task<OperationOutcome> Handle(ClearCache command, CancellationToken ct)
    {
        await this.cache.ClearAsync(ct);
        return new Success();
    }
}
```

### NotFound

Entity does not exist:

```csharp
public record NotFound(string? Message = null);

public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

var result = await mediator.Send(new GetUser(999));
var notFound = Assert.IsType<NotFound>(result.Value);
Assert.NotNull(notFound.Message);
```

### Forbidden

Authorization denied for a resource:

```csharp
public record Forbidden(string? Reason = null);

public record DeleteDocument(int DocumentId)
    : ICommand<OperationOutcome<Success, NotFound, Forbidden>>;

var result = await mediator.Send(new DeleteDocument(42));
var failure = Assert.IsAssignableFrom<Failure>(result.Value);
```

### ValidationError

Input validation failure. Produced by `ValidationBehavior` when FluentValidation rules fail:

```csharp
public record ValidationError(string Message, string? Code = null);

// Aggregated
public record ValidationErrors(IReadOnlyList<ValidationError> Items);

var result = await mediator.Send(new CreateUser("", "bad-email"));
// result.Value is ValidationError with collected errors
```

### Conflict

State conflict - concurrency, uniqueness violation, invalid transition:

```csharp
public record Conflict(string Code, string? Message = null);

public record UpdateProduct(int ProductId, string Name, int Version)
    : ICommand<OperationOutcome<Success, NotFound, Conflict>>;

var result = await mediator.Send(new UpdateProduct(1, "New Name", 3));
// If another request updated the product (version mismatch), result is Conflict
```

### Failure

Generic fallback failure when none of the specific cases apply. `Failure` is reason-only; `Failure<TErrorCode>` adds a machine-readable error code, with `TErrorCode` fixed for the lifetime of the value:

```csharp
public readonly record struct Failure(string? Reason = null);

public readonly record struct Failure<TErrorCode>(TErrorCode ErrorCode, string? Reason = null)
    where TErrorCode : notnull, IEquatable<TErrorCode>;

public record GetSetting(string Key) : IQuery<OperationOutcome<string, Failure>>;

var result = await mediator.Send(new GetSetting("unknown.key"));
// result.Value is Failure("Setting not found")
```

Aggregate multiple failures with `Failures` (or `Failures<TErrorCode>` for the error-code variant) when a handler can fail for more than one independent reason at once:

```csharp
public record BulkDelete(IReadOnlyList<int> Ids) : ICommand<OperationOutcome<Success, Failures>>;

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
    : ICommand<OperationOutcome<Success, Cancelled, Timeout, RateLimited>>, ICancellable, IHasTimeout, IRateLimited
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

Results are C# 15 discriminated unions:

```csharp
// Two-case union: success or generic failure
public readonly union OperationOutcome(Success, Failure);

// Generic union: success + specific failure cases
public readonly union OperationOutcome<TSuccess, TFailure>(TSuccess, TFailure);

// Wider arities (2 to 6 cases)
public readonly union OperationOutcome<TSuccess, TCase2, TCase3>(
    TSuccess, TCase2, TCase3);
```

### Pattern Matching

Use `switch` expressions to handle each case:

```csharp
var result = await mediator.Send(new DeleteUser(42));
// result is OperationOutcome<Success, NotFound, Forbidden>

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

## Choosing Arities

Don't route every failure through a single `Failure`. Pick the arity that an operation can actually produce:

```csharp
// ✅ Specific - only NotFound and Failure are possible
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound>>;

// ❌ Over-broad - ValidationError should never occur (read-only query)
public record GetUser(int UserId) : IQuery<OperationOutcome<UserDto, NotFound, ValidationError>>;

// ✅ Specific - a command can fail with input validation or a concurrency conflict
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<OperationOutcome<Success, ValidationError, Conflict>>;

// ❌ Ambiguous - operation produces either a value or one of many failures, lose specificity
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<OperationOutcome<Success, Failure>>;
```

When a handler's response arity is specific (only the cases it can produce), the compiler forces callers to handle exactly those cases - no more, no less.

## Marker Interfaces

Requests can opt into pipeline behaviors via marker interfaces:

| Interface | Used by | Carries |
|-----------|---------|---------|
| `ITransactionalRequest` | `UnitOfWorkBehavior` (forces an eager transaction begin - every write already gets one automatically) | - |
| `IExternalStoreRequest` | Declares "no SQL session" | - |
| `IAuditableRequest` | `AuditingBehavior` | `ActionName` |
| `IRequiresPolicy` | `AuthorizationBehavior` | `PolicyName` |

A request can implement multiple markers:

```csharp
public record TransferFunds(int FromAccountId, int ToAccountId, decimal Amount)
    : ICommand<OperationOutcome<Success, ValidationError, InsufficientFunds>>,
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

The response type parameter appears in **both argument and return position** on `IPipelineBehavior<TRequest, TResponse>` and lives inside a `Task` on handler interfaces:

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
    // TResponse is in return position (covariance)
    // AND inside Func<Task<TResponse>> - an argument position (contravariance)
}

public interface IQueryHandler<TQuery, TResponse> where TResponse : IOperationOutcome
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
public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<Admin, NotFound>>
{
}

var handler = new GetUserHandler();  // OperationOutcome<Admin, NotFound>
IQueryHandler<GetUser, OperationOutcome<User, NotFound>> general = handler;  // ❌ Type mismatch

// ✅ Must be explicit (or use the exact same type in both places)
public class GetUserHandler : IQueryHandler<GetUser, OperationOutcome<User, NotFound>>
{
}
```

Polymorphism is request-side only. Queries and commands can inherit from a base request type (contravariant on `TRequest`), but the response type is invariant across every interface in the library.

## Streaming: Deliberately Not Supported

There is no `IAsyncEnumerable<T>` / streaming response type - streaming and "commit/rollback as one unit" don't compose, so it's deliberately kept out permanently, not a gap waiting to be filled.

The constraint (all responses must be `IOperationOutcome`) ensures:

- A caller can inspect success/failure without knowing the concrete outcome shape (enables runtime-typed dispatch).
- Transactions can guarantee "all or nothing" - a single commit/rollback boundary.
- Pipeline behaviors can compose generically over `TResponse`.

Streaming violates both: a partial stream can fail halfway, the caller might not consume the stream, and the transaction boundary becomes unclear.

Use materialized collections instead:

```csharp
// ✅ Correct - materialized list, one outcome
public record GetUsers() : IQuery<OperationOutcome<IReadOnlyList<UserDto>, Failure>>;

public class GetUsersHandler : IQueryHandler<GetUsers, OperationOutcome<IReadOnlyList<UserDto>, Failure>>
{
    public async Task<OperationOutcome<IReadOnlyList<UserDto>, Failure>> Handle(
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
    : IQuery<OperationOutcome<PagedResult<UserDto>, Failure>>;

var result = await mediator.Send(new GetUsers(1, 10));
// result.Value is a PagedResult with Items, Page, PageSize, TotalCount
```
