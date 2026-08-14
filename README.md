# Interlocutor

A mediator/CQRS pipeline library for .NET built on C# 15 union types.

Named after the participant who conducts a dialogue — the library mediates requests between your code and its handlers, letting each side speak through typed, compiler-checked outcomes (succeeded, not found, forbidden, etc.) instead of throwing for expected failures. Exceptions stay reserved for bugs and infrastructure faults.

## Quick Start

### Setup

Register Interlocutor with dependency injection and configure pipeline behaviors:

```csharp
// Startup.cs or Program.cs
services.AddInterlocutor(typeof(Program).Assembly);  // Auto-scan and register all handlers

// Optional: add built-in pipeline behaviors (in recommended order)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,,>));

// Optional: configure which DI container to use
// For Autofac: services.AddInterlocutor(config => config.UseAutofac(containerBuilder));
```

### Define and Send Requests

Queries read data; commands mutate state:

```csharp
// 1. Define a query — TResponse can be any type
public record GetUser(int UserId) : IQuery<UserResponse>;

// 2. Implement a handler — dependency injection works normally
public class GetUserHandler : IQueryHandler<GetUser, UserResponse>
{
    private readonly IUserRepository repository;

    public GetUserHandler(IUserRepository repository) => this.repository = repository;

    public async Task<UserResponse> Handle(GetUser query, CancellationToken ct)
    {
        var user = await repository.FindAsync(query.UserId, ct);
        return new UserResponse(user?.Name ?? "Unknown");
    }
}

// 3. Send it via ISender or IMediator
var user = await mediator.Send(new GetUser(1));
```

### Handle Results

Result types are unions — use pattern matching to handle each case:

```csharp
public record DeleteUser(int UserId) : ICommand<OperationOutcome<Success, NotFound, Forbidden>>;

public class DeleteUserHandler : ICommandHandler<DeleteUser, OperationOutcome<Success, NotFound, Forbidden>>
{
    private readonly IUserRepository repository;

    public async Task<OperationOutcome<Success, NotFound, Forbidden>> Handle(
        DeleteUser command, CancellationToken ct)
    {
        var user = await repository.FindAsync(command.UserId, ct);
        if (user is null)
            return new NotFound($"User {command.UserId} not found");

        if (!user.CanBeDeletedBy(CurrentUser))
            return new Forbidden("Only admins can delete users");

        await repository.DeleteAsync(user, ct);
        return new Success();
    }
}

// Caller side — pattern match on the union
var result = await mediator.Send(new DeleteUser(42));
var message = result.Value switch
{
    Success => "User deleted successfully",
    NotFound failure => $"Error: {failure.Message}",
    Forbidden failure => $"Access denied: {failure.Message}",
};
```

## Packages

| Package | Purpose |
|---------|---------|
| `Interlocutor.CommonKernel` | Ready-made result case types (`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, `ValidationFailure`, `Success`) and the `OperationOutcome<...>` union arities. Zero dependencies. |
| `Interlocutor.Abstractions` | Core mediator contracts, handler interfaces, pipeline behaviors, Mediator implementation. |
| `Interlocutor.AspNetCore` | `ToActionResult()` HTTP mapping for ASP.NET Core. |
| `Interlocutor.DependencyInjection.Microsoft` | Assembly-scanning `AddInterlocutor()` for Microsoft.Extensions.DependencyInjection. |
| `Interlocutor.DependencyInjection.Autofac` | Assembly-scanning `AddInterlocutor()` for Autofac. |
| `Interlocutor.EntityFrameworkCore` | `TransactionBehavior` for EF Core `DbContext`. |
| `Interlocutor.NHibernate` | `TransactionBehavior` for NHibernate `ISession`. |
| `Interlocutor.FluentValidation` | `ValidationBehavior` and `BulkItemValidationBehavior`. |

## Result Types Are Building Blocks, Not a Mandatory Wrapper

Every handler interface (`IQueryHandler<,>`, `ICommandHandler<,>`) and `IPipelineBehavior<,>` is generic over a plain `TResponse` — there is no forced `OperationOutcome<TResponse>` wrapper. `TResponse` can be:

- a plain type (`UserResponse`, `int`, `string`) when the operation can't meaningfully fail,
- a `Interlocutor.CommonKernel.ResultTypes` union, or
- a union type your own application defines.

`Interlocutor.CommonKernel.ResultTypes` ships a two-case union and three wider arities:

```csharp
public readonly union OperationOutcome<TSuccess, TFailure>(TSuccess, TFailure);
public readonly union OperationOutcome(Success, Failure);
public readonly union OperationOutcome<TSuccess, TCase2, TCase3>(TSuccess, TCase2, TCase3);
public readonly union OperationOutcome<TSuccess, TCase2, TCase3, TCase4>(TSuccess, TCase2, TCase3, TCase4);
public readonly union OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5>(TSuccess, TCase2, TCase3, TCase4, TCase5);
```

`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, and `ValidationFailure` are independent, standalone case types — none of them is an aggregate of the others. Pick whichever combination a handler actually needs as type arguments to `OperationOutcome<...>`; don't route every failure through a single generic `Failure` if the caller needs to distinguish cases.

### Why compose it this way

Picking your own failure cases per handler, rather than a single `OperationOutcome<T>` funneling everything through one `Failure` type, means the compiler forces every call site to handle exactly the failure modes that operation can actually produce — no more, no less. A query that can only fail with "not found" shouldn't force callers to also handle "forbidden."

```csharp
// Single generic failure — fine when the caller only needs "it didn't work, here's why."
public record GetSetting(string Key) : IQuery<OperationOutcome<string, Failure>>;

public class GetSettingHandler : IQueryHandler<GetSetting, OperationOutcome<string, Failure>>
{
    public Task<OperationOutcome<string, Failure>> Handle(GetSetting query, CancellationToken ct) =>
        Task.FromResult<OperationOutcome<string, Failure>>(
            settings.TryGetValue(query.Key, out var value)
                ? value
                : new Failure($"No setting named '{query.Key}'")
        );
}

// Multiple distinct failure modes — the switch below won't compile until every
// arm is handled, so adding a new case type later surfaces every call site that
// needs updating.
public record DeleteOrder(int OrderId) : ICommand<OperationOutcome<Success, NotFound, Forbidden>>;

public class DeleteOrderHandler : ICommandHandler<DeleteOrder, OperationOutcome<Success, NotFound, Forbidden>>
{
    public async Task<OperationOutcome<Success, NotFound, Forbidden>> Handle(DeleteOrder command, CancellationToken ct)
    {
        var order = await repository.FindAsync(command.OrderId, ct);
        if (order is null)
        {
            return new NotFound($"Order {command.OrderId} not found");
        }

        if (!order.CanBeDeletedBy(currentUser))
        {
            return new Forbidden("Only the order owner can delete it");
        }

        await repository.DeleteAsync(order, ct);
        return new Success();
    }
}

// A custom union — Interlocutor doesn't require CommonKernel's shapes at all.
public union PaymentOutcome(Charged, InsufficientFunds, CardDeclined);
public record ProcessPayment(decimal Amount) : ICommand<PaymentOutcome>;
```

`ResourceCommandHandler<TCommand, TEntity, TSuccess>` is a template method for the common "load → authorize → act" shape. It always returns `OperationOutcome<TSuccess, NotFound, Failure>` because it needs to construct those two specific failure cases itself:

```csharp
public record ArchiveDocument(int DocumentId, int RequestedBy) : ICommand<OperationOutcome<bool, NotFound, Failure>>;

public class ArchiveDocumentHandler : ResourceCommandHandler<ArchiveDocument, Document, bool>
{
    private readonly IDocumentRepository repository;

    public ArchiveDocumentHandler(IDocumentRepository repository) => this.repository = repository;

    protected override Task<Document?> LoadAsync(ArchiveDocument command, CancellationToken ct) =>
        repository.FindAsync(command.DocumentId, ct);

    protected override Failure? Authorize(Document entity, ArchiveDocument command) =>
        entity.OwnerId == command.RequestedBy ? null : new Failure("Not the document owner");

    protected override Task<OperationOutcome<bool, NotFound, Failure>> HandleAsync(
        ArchiveDocument command, Document entity, CancellationToken ct)
    {
        entity.Archive();
        return Task.FromResult<OperationOutcome<bool, NotFound, Failure>>(true);
    }
}
```

## Composing Pipeline Behaviors

Behaviors wrap the handler in an onion/Russian-doll model via `IPipelineBehavior<TRequest, TResponse>`. **Registration order is execution order from the outside in**: the first behavior registered runs first on the way in and last on the way out; the last one registered sits closest to the handler.

```csharp
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,,>));
```

Why this order:

- **`LoggingBehavior` outermost** — it times and logs the *whole* request, including time spent in authorization or validation failures further in. If it sat closer to the handler, a denied-by-policy request would never be logged.
- **`AuditingBehavior` right after logging** — an audit trail should record denials and validation failures too, not just successful business operations.
- **`AuthorizationBehavior` before `ValidationBehavior`** — there's no reason to validate the shape of a request the caller isn't even allowed to make; failing fast on authorization saves the validator's work.
- **`ValidationBehavior` before `TransactionBehavior`** — don't open a database transaction for a request that's going to be rejected for invalid input.
- **`TransactionBehavior` innermost** — it should wrap only the actual persistence work, committing on success and rolling back only when the handler itself fails, not when an outer behavior short-circuits.

`LoggingBehavior`, `AuditingBehavior`, and both `TransactionBehavior`s use an injectable `IResponseOutcomeDescriber` to inspect the response generically — it unwraps compiler-generated unions via `IUnion.Value` and pattern-matches the well-known CommonKernel case types, falling back to the runtime type name for anything else. Register your own implementation to customize how these behaviors describe or classify outcomes for a custom union type:

```csharp
services.AddSingleton<IResponseOutcomeDescriber, PaymentOutcomeDescriber>();
```

`AuthorizationBehavior<TRequest, TSuccess>` and `ValidationBehavior<TRequest, TSuccess>` are constrained to the concrete shapes `OperationOutcome<TSuccess, Forbidden>` and `OperationOutcome<TSuccess, ValidationFailed>` respectively — a short-circuiting behavior has to construct its failure case without knowing the handler's response shape in general, so it's opt-in *and* only usable when the response shape matches. Handlers with a different response shape (a custom union, or a wider multi-arity `OperationOutcome<...>`) need a bespoke behavior instead.

## Publishing Notifications

Fire-and-forget notifications are handled separately from commands/queries:

```csharp
// Define a notification
public record OrderShipped(int OrderId, string TrackingNumber) : INotification;

// Implement one or more handlers — they run sequentially
public class OrderShippedEmailHandler : INotificationHandler<OrderShipped>
{
    private readonly IEmailService emailService;

    public OrderShippedEmailHandler(IEmailService emailService) => this.emailService = emailService;

    public async Task Handle(OrderShipped notification, CancellationToken ct)
    {
        await emailService.SendAsync(
            "order-shipped",
            tracking: notification.TrackingNumber,
            ct
        );
    }
}

// Publish — all handlers run sequentially
await mediator.Publish(new OrderShipped(123, "TRACK-456"));
```

## Validation

Add FluentValidation integration for automatic request validation:

```csharp
// Define a request with validators
public record CreateUser(string Name, string Email) : ICommand<OperationOutcome<int, ValidationFailed>>;

public class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).EmailAddress();
    }
}

// ValidationBehavior runs before your handler and returns ValidationFailed if invalid
var result = await mediator.Send(new CreateUser("", "not-an-email"));
// result.Value is ValidationFailed with collected errors
```

## Authorization

Protect handlers with policy-based authorization:

```csharp
// Mark a request as requiring authorization
public record DeleteAllUsers() : ICommand<Success>, IRequiresPolicy;

// Implement authorization logic
public class AdminOnlyPolicyEvaluator : IPolicyEvaluator
{
    private readonly ICurrentUser currentUser;

    public AdminOnlyPolicyEvaluator(ICurrentUser currentUser) => this.currentUser = currentUser;

    public Task<Forbidden?> EvaluateAsync(IRequiresPolicy request, CancellationToken ct) =>
        Task.FromResult(
            currentUser.IsAdmin ? null : new Forbidden("Admin access required")
        );
}

// AuthorizationBehavior runs before validation and short-circuits if denied
var result = await mediator.Send(new DeleteAllUsers());
```

## Auditing

Track all business operations:

```csharp
// Mark requests for auditing
public record TransferFunds(int FromAccountId, int ToAccountId, decimal Amount)
    : ICommand<Success>, IAuditableRequest;

// Implement audit sink to store audit trails
public class DatabaseAuditSink : IAuditSink
{
    private readonly IAuditLogRepository auditRepo;

    public async Task RecordAsync(AuditEntry entry, CancellationToken ct)
    {
        await auditRepo.AddAsync(new()
        {
            RequestType = entry.RequestType,
            Timestamp = entry.Timestamp,
            ElapsedMs = entry.ElapsedMilliseconds,
            UserId = entry.UserId,
            Outcome = entry.Outcome,
            Details = entry.Details,
        }, ct);
    }
}
```

## Database Transactions

Automatically wrap handlers in transactions:

```csharp
// Mark a command for transactional handling
public record CreateOrder(int CustomerId, List<OrderItem> Items)
    : ICommand<OperationOutcome<int, ValidationFailed>>, ITransactionalRequest;

// TransactionBehavior (registered last) wraps the handler:
// - Commits on Success
// - Rolls back on any Failure case
// - Never rolls back on authorization/validation short-circuit
var result = await mediator.Send(new CreateOrder(1, items));
```

## Testing

Handlers are just classes — test them in isolation:

```csharp
[Fact]
public async Task DeleteUser_WithAdmin_Succeeds()
{
    var repository = new Mock<IUserRepository>();
    repository.Setup(r => r.FindAsync(1, default))
        .ReturnsAsync(new User { Id = 1, Name = "Alice" });

    var handler = new DeleteUserHandler(repository.Object);
    var result = await handler.Handle(new DeleteUser(1), CancellationToken.None);

    Assert.True(result.IsSuccess);
    repository.Verify(r => r.DeleteAsync(It.IsAny<User>(), default), Times.Once);
}

[Fact]
public async Task DeleteUser_NotFound_ReturnsFalse()
{
    var repository = new Mock<IUserRepository>();
    repository.Setup(r => r.FindAsync(999, default))
        .ReturnsAsync((User?)null);

    var handler = new DeleteUserHandler(repository.Object);
    var result = await handler.Handle(new DeleteUser(999), CancellationToken.None);

    var notFound = Assert.IsType<NotFound>(result.Value);
    Assert.Contains("not found", notFound.Message);
}
```

## ASP.NET Core Integration

Map handler results to HTTP responses:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ISender mediator;

    public UsersController(ISender mediator) => this.mediator = mediator;

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteUser(id), ct);

        // Built-in mapping: NotFound → 404, Forbidden → 403, etc.
        return result.ToActionResult();
    }
}

// For minimal APIs:
app.MapDelete("/api/users/{id}", async (int id, ISender mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteUser(id), ct);
    return result.ToActionResult();  // Same mapping logic
});
```

## Interface Segregation

Inject only what you need:

- `ISender` — Send-only (request/response)
- `IPublisher` — Publish-only (fire-and-forget notifications)
- `IMediator` — Combined convenience interface

## Performance

- Handlers are registered as singletons (stateless) when possible, otherwise as scoped/transient per your registration.
- Pipeline behaviors are scoped to the request by default.
- Use `AddInterlocutor(Type)` for single-assembly registration or `AddInterlocutorFromEntryAssembly()` to scan from the entry point.
- For large request volumes, consider disabling logging/auditing behaviors or implementing conditional registration.

## Links

- [PRD](docs/PRD.md)
- [Architecture Decision Records](docs/adr/)
