# Mediance

A mediator/CQRS pipeline library for .NET built on C# 15 union types.

Named after the legal term for how a case is resolved — handlers return outcomes (succeeded, not found, forbidden, etc.) as compiler-checked unions instead of throwing for expected failures. Exceptions stay reserved for bugs and infrastructure faults.

## Quick Start

```csharp
// 1. Register Mediance
services.AddMediance(typeof(GetUserHandler).Assembly);

// 2. Define a query. TResponse can be a plain type or a union — the mediator
//    doesn't force any particular shape onto it.
public record GetUser(int UserId) : IQuery<UserResponse>;

// 3. Implement the handler
public class GetUserHandler : IQueryHandler<GetUser, UserResponse>
{
    public Task<UserResponse> Handle(GetUser query, CancellationToken ct) =>
        Task.FromResult(new UserResponse("Alice"));
}

// 4. Send it
var user = await mediator.Send(new GetUser(1));
```

## Packages

| Package | Purpose |
|---------|---------|
| `Mediance.CommonKernel` | Ready-made result case types (`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, `ValidationFailure`, `Success`) and the `Result<...>` union arities. Zero dependencies. |
| `Mediance.Abstractions` | Core mediator contracts, handler interfaces, pipeline behaviors, Mediator implementation. |
| `Mediance.AspNetCore` | `ToActionResult()` HTTP mapping for ASP.NET Core. |
| `Mediance.DependencyInjection.Microsoft` | Assembly-scanning `AddMediance()` for Microsoft.Extensions.DependencyInjection. |
| `Mediance.DependencyInjection.Autofac` | Assembly-scanning `AddMediance()` for Autofac. |
| `Mediance.EntityFrameworkCore` | `TransactionBehavior` for EF Core `DbContext`. |
| `Mediance.NHibernate` | `TransactionBehavior` for NHibernate `ISession`. |
| `Mediance.FluentValidation` | `ValidationBehavior` and `BulkItemValidationBehavior`. |

## Result Types Are Building Blocks, Not a Mandatory Wrapper

Every handler interface (`IQueryHandler<,>`, `ICommandHandler<,>`) and `IPipelineBehavior<,>` is generic over a plain `TResponse` — there is no forced `Result<TResponse>` wrapper. `TResponse` can be:

- a plain type (`UserResponse`, `int`, `string`) when the operation can't meaningfully fail,
- a `Mediance.CommonKernel.ResultTypes` union, or
- a union type your own application defines.

`Mediance.CommonKernel.ResultTypes` ships a two-case union and three wider arities:

```csharp
public readonly union Result<TSuccess, TFailure>(TSuccess, TFailure);
public readonly union Result(Success, Failure);
public readonly union Result<TSuccess, T2, T3>(TSuccess, T2, T3);
public readonly union Result<TSuccess, T2, T3, T4>(TSuccess, T2, T3, T4);
public readonly union Result<TSuccess, T2, T3, T4, T5>(TSuccess, T2, T3, T4, T5);
```

`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, and `ValidationFailure` are independent, standalone case types — none of them is an aggregate of the others. Pick whichever combination a handler actually needs as type arguments to `Result<...>`; don't route every failure through a single generic `Failure` if the caller needs to distinguish cases.

### Why compose it this way

Picking your own failure cases per handler, rather than a single `Result<T>` funneling everything through one `Failure` type, means the compiler forces every call site to handle exactly the failure modes that operation can actually produce — no more, no less. A query that can only fail with "not found" shouldn't force callers to also handle "forbidden."

```csharp
// Single generic failure — fine when the caller only needs "it didn't work, here's why."
public record GetSetting(string Key) : IQuery<Result<string, Failure>>;

public class GetSettingHandler : IQueryHandler<GetSetting, Result<string, Failure>>
{
    public Task<Result<string, Failure>> Handle(GetSetting query, CancellationToken ct) =>
        Task.FromResult<Result<string, Failure>>(
            settings.TryGetValue(query.Key, out var value)
                ? value
                : new Failure($"No setting named '{query.Key}'")
        );
}

// Multiple distinct failure modes — the switch below won't compile until every
// arm is handled, so adding a new case type later surfaces every call site that
// needs updating.
public record DeleteOrder(int OrderId) : ICommand<Result<Success, NotFound, Forbidden>>;

public class DeleteOrderHandler : ICommandHandler<DeleteOrder, Result<Success, NotFound, Forbidden>>
{
    public async Task<Result<Success, NotFound, Forbidden>> Handle(DeleteOrder command, CancellationToken ct)
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

// A custom union — Mediance doesn't require CommonKernel's shapes at all.
public union PaymentOutcome(Charged, InsufficientFunds, CardDeclined);
public record ProcessPayment(decimal Amount) : ICommand<PaymentOutcome>;
```

`ResourceCommandHandler<TCommand, TEntity, TSuccess>` is a template method for the common "load → authorize → act" shape, and is fixed to `Result<TSuccess, NotFound, Failure>` because it needs to construct those two specific failure cases itself:

```csharp
public class ArchiveDocumentHandler : ResourceCommandHandler<ArchiveDocument, Document, Success>
{
    protected override Task<Document?> LoadAsync(ArchiveDocument command, CancellationToken ct) =>
        repository.FindAsync(command.DocumentId, ct);

    protected override Failure? Authorize(Document entity, ArchiveDocument command) =>
        entity.OwnerId == command.RequestedBy ? null : new Failure("Not the document owner");

    protected override Task<Result<Success, NotFound, Failure>> HandleAsync(
        ArchiveDocument command, Document entity, CancellationToken ct)
    {
        entity.Archive();
        return Task.FromResult<Result<Success, NotFound, Failure>>(new Success());
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

`AuthorizationBehavior<TRequest, TSuccess>` and `ValidationBehavior<TRequest, TSuccess>` are constrained to the concrete shapes `Result<TSuccess, Forbidden>` and `Result<TSuccess, ValidationFailed>` respectively — a short-circuiting behavior has to construct its failure case without knowing the handler's response shape in general, so it's opt-in *and* only usable when the response shape matches. Handlers with a different response shape (a custom union, or a wider multi-arity `Result<...>`) need a bespoke behavior instead.

## Interface Segregation

- `ISender` — Send-only (request/response)
- `IPublisher` — Publish-only (fire-and-forget notifications)
- `IMediator` — Combined convenience interface

## Links

- [PRD](docs/PRD.md)
- [Architecture Decision Records](docs/adr/)
