# Transactions & Store Integration

Managing transactions with `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate`. Because the mediator is scoped, each dispatch gets exactly one store session - a lookup and a write inside the same command execute against the same session. `IUnitOfWork` owns that session's transaction: it opens lazily, the first time anything actually writes, and commits or rolls back automatically once the dispatch completes.

## UnitOfWorkBehavior

Both EF Core and NHibernate ship a `UnitOfWorkBehavior` that owns transaction lifecycle for every request it's registered against - no marker required to participate:

```csharp
// No ITransactionalRequest needed - the write below gets a transaction automatically.
public record CreateOrder(int CustomerId, List<OrderItem> Items)
    : ICommand<OperationOutcome<int, ValidationError>>;

// When the handler runs, UnitOfWorkBehavior:
// 1. Lets reads run against the scoped session with no transaction open
// 2. Opens a transaction the first time anything writes (IDomainRepository, IUnitOfWork,
//    or a directly-injected Lazy<TContext>/Lazy<ISession>)
// 3. Commits on Success
// 4. Rolls back on any Failure case
```

A dispatch that only ever reads never pays for an open transaction - it never opens one at all.

## ITransactionalRequest Marker

`ITransactionalRequest` no longer means "this request wants a transaction" - every write already gets one, automatically. It narrows to: force the transaction to begin *eagerly*, at pipeline start, instead of waiting for the first write - for the check-then-act case where a read must be protected by the same transaction as a write that follows it:

```csharp
// A duplicate check immediately before an insert - without an eager transaction, a concurrent
// request could pass the same check before either insert commits.
public record CreateOrder(int CustomerId, List<OrderItem> Items)
    : ICommand<OperationOutcome<int, ValidationError>>,
      ITransactionalRequest;

// A plain create with no preceding read to protect doesn't need the marker at all - begin-on-first-write
// already covers it.
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<OperationOutcome<Success, Conflict>>;

// Queries never write, so a transaction never opens for one regardless of this marker.
public record GetProduct(int Id)
    : IQuery<OperationOutcome<ProductDto, NotFound>>;
```

`ITransactionalRequest` only has an effect when `UnitOfWorkBehavior` sits *outside* whatever pipeline stage or handler code performs the protected read - see "Where to register the behavior" below.

## IExternalStoreRequest Marker

Declare a command is backed by a non-SQL store so `UnitOfWorkBehavior` never opens a local transaction around it, even if the handler happens to touch the scoped session:

```csharp
// This command uses a NoSQL store (Redis, MongoDB, etc.)
public record SetUserPreferences(int UserId, Dictionary<string, object> Prefs)
    : ICommand<Success>,
      IExternalStoreRequest;  // ← Never gets a local transaction

// UnitOfWorkBehavior sees this marker and skips transaction management entirely
// but other behaviors (validation, auditing) still run
```

> [!IMPORTANT]
> `UnitOfWorkBehavior` and the `IUnitOfWork`/`IDomainRepository<T>` implementations all use `Lazy<TContext>`/`Lazy<ISession>` - a dispatch that never writes never opens a transaction, and one that never touches storage at all never even constructs the session. This is critical for performance in applications with mixed transactional/non-transactional work.

## EntityFramework Core Integration

`Interlocutor.Persistence.EntityFrameworkCore`'s `UnitOfWorkBehavior<TRequest, TResponse, TContext>` is generic over the `DbContext` type too (arity 3), unlike NHibernate's (arity 2, below) - `ISession` isn't generic, so it doesn't need that third parameter. Register it with `AddUnitOfWorkBehavior()`, which probes for whichever store package is referenced and closes it correctly:

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    // Add the unit-of-work behavior (requires DbContext in DI) - see the Autofac/Microsoft DI
    // note below before using this under Microsoft.Extensions.DependencyInjection.
    builder.Pipeline.AddUnitOfWorkBehavior();
});

// DbContext must be registered as scoped (usually already is)
services.AddDbContext<AppDbContext>(
    options => options.UseSqlServer(/* ... */),
    ServiceLifetime.Scoped
);
```

> [!WARNING]
> **Under Microsoft.Extensions.DependencyInjection specifically, `AddUnitOfWorkBehavior()` currently throws at container-build time when `Interlocutor.Persistence.EntityFrameworkCore` is referenced** - `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,,>))` fails because Microsoft DI requires an open generic implementation's arity to match its service interface's arity exactly, and `UnitOfWorkBehavior<,,>`'s extra `TContext` parameter breaks that. Autofac's generic registration doesn't have this restriction, so `AddUnitOfWorkBehavior()` works there unmodified today.
>
> Until this is fixed, register a small subclass under Microsoft DI that closes `TContext` over your own `DbContext` type, then register that subclass through the general-purpose `AddBehavior` escape hatch instead of `AddUnitOfWorkBehavior()`:
>
> ```csharp
> // Closes TContext over AppDbContext, bringing the arity back down to 2
> internal sealed class AppUnitOfWorkBehavior<TRequest, TResponse>(
>     Lazy<AppDbContext> context,
>     IOutcomeClassifier classifier,
>     IResponseOutcomeDescriber? describer = null
> ) : UnitOfWorkBehavior<TRequest, TResponse, AppDbContext>(context, classifier, describer);
>
> services.AddInterlocutor(builder =>
> {
>     builder.ScanAssembly(typeof(Program).Assembly);
>     builder.Pipeline.AddBehavior(typeof(AppUnitOfWorkBehavior<,>));  // not AddUnitOfWorkBehavior()
> });
> ```
>
> This is the same pattern the runnable sample under [`samples/Interlocutor.Sample.WebApi`](../samples/Interlocutor.Sample.WebApi/SampleUnitOfWorkBehavior.cs) uses, verified against a real `ServiceProvider`.

### Whole-session protection for direct `DbContext` access

Nothing reaches the database before EF Core's `SaveChanges`/`SaveChangesAsync` runs, regardless of how entities were staged - `IDomainRepository`, `IUnitOfWork`, or a handler's own directly-injected `Lazy<AppDbContext>`. `UnitOfWorkBehavior` and `IUnitOfWork` both protect the first two paths on their own. To close the third - a handler calling `context.SaveChangesAsync()` itself - wire `UnitOfWorkSaveChangesInterceptor` into the same `AddDbContext` call:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(/* ... */)
        .AddInterceptors(new UnitOfWorkSaveChangesInterceptor())  // stateless - one shared instance is fine
);
```

Without it, a raw `SaveChangesAsync()` call with no transaction already open commits immediately via EF Core's own per-call implicit transaction, uncontrolled by the dispatch's later commit/rollback decision.

The behavior will inject `Lazy<AppDbContext>` and resolve it only when needed:

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrder, OperationOutcome<int, ValidationError>>
{
    private readonly Lazy<AppDbContext> lazyContext;  // Injected by DI

    public CreateOrderHandler(Lazy<AppDbContext> lazyContext)
        => this.lazyContext = lazyContext;

    public async Task<OperationOutcome<int, ValidationError>> Handle(
        CreateOrder command, CancellationToken ct)
    {
        var context = this.lazyContext.Value;  // Session is resolved here; the write below opens the transaction

        var order = new Order { CustomerId = command.CustomerId, /* ... */ };
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);  // Committed by UnitOfWorkBehavior (if no Failure)

        return order.Id;
    }
}
```

## NHibernate Integration

Register the NHibernate `UnitOfWorkBehavior` the same way - `AddUnitOfWorkBehavior()` works unmodified here on both Microsoft DI and Autofac, since `ISession` isn't generic and the behavior stays arity 2:

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    // Add the unit-of-work behavior (requires ISession factory in DI)
    builder.Pipeline.AddUnitOfWorkBehavior();
});

// NHibernate session factory must be registered
services.AddSingleton(sessionFactory);  // ISessionFactory
services.AddScoped<ISession>(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());
```

`UnitOfWorkBehavior` sets the session's `FlushMode` to `Commit` at pipeline start (unless the request is an `IExternalStoreRequest`), so NHibernate's default auto-flush-before-query can never write staged changes to the database ahead of a transaction this behavior opens around them - this is the one piece of richer NHibernate access begin-on-first-write depends on.

NHibernate behavior works the same way - scoped `ISession` is shared per dispatch and transacted:

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrder, OperationOutcome<int, ValidationError>>
{
    private readonly Lazy<ISession> lazySession;

    public CreateOrderHandler(Lazy<ISession> lazySession)
        => this.lazySession = lazySession;

    public async Task<OperationOutcome<int, ValidationError>> Handle(
        CreateOrder command, CancellationToken ct)
    {
        var session = this.lazySession.Value;  // Session is resolved here

        var order = new Order { CustomerId = command.CustomerId, /* ... */ };
        await session.SaveAsync(order, ct);
        await session.FlushAsync(ct);  // Opens a transaction first if one isn't already open, then flushes

        return order.Id;  // Committed by UnitOfWorkBehavior (if no Failure)
    }
}
```

## Cancel: forcing a rollback without a failure outcome

`IUnitOfWork.Cancel` is a rare-case escape hatch: it rolls back the current transaction immediately and returns control to the caller, without being obligated to also return a classified failure-shaped `OperationOutcome`. It is not a short-circuit primitive - the handler still decides what to return afterward, and the pipeline's commit gate simply finds nothing pending at completion:

```csharp
public class ArchiveOrderHandler : ICommandHandler<ArchiveOrder, OperationOutcome<Success>>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly Lazy<AppDbContext> lazyContext;

    public async Task<OperationOutcome<Success>> Handle(ArchiveOrder command, CancellationToken ct)
    {
        var context = this.lazyContext.Value;
        context.Orders.Remove(/* ... */);

        if (await ExternalArchiveWasAlreadyRunning(command.OrderId, ct))
        {
            // Force a rollback of whatever this handler staged, then decide the outcome independently -
            // UnitOfWorkBehavior finds nothing open or pending and does nothing further.
            await this.unitOfWork.Cancel(ct);
            return default(Success);  // Still reports success: the archive already happened elsewhere.
        }

        await this.unitOfWork.SaveChangesAsync(ct);
        return default(Success);
    }
}
```

## Commit/Rollback Semantics

- **Never opens for a pure read**: a dispatch that never writes never opens a transaction at all - no marker to remember, no cost to pay.
- **Begins on first write**: the transaction opens the moment anything writes - through `IDomainRepository`, `IUnitOfWork`, or a directly-injected `Lazy<TContext>`/`Lazy<ISession>` - not at dispatch start.
- **Begins eagerly for `ITransactionalRequest`**: at pipeline start instead, so an earlier protective read shares the same transaction as the write that follows it.
- **Commit on Success**: transaction commits when the handler returns `Success` or a success-type case.
- **Rollback on Failure**: transaction rolls back when the handler returns any `Failure` case (`NotFound`, `Forbidden`, `Conflict`, `ValidationError`, etc.), or when the handler calls `IUnitOfWork.Cancel`.
- **No rollback on pipeline short-circuit**: if `AuthorizationBehavior` or `ValidationBehavior` short-circuits a request before any write happens, no transaction was ever opened. No transaction to roll back.

The last two rules are the ones that are easy to misread as "any denied or failed request rolls back" - it doesn't, when nothing was ever written in the first place:

```mermaid
flowchart TD
    Start["Request dispatched"] --> ShortCircuit{"Short-circuited before<br/>any write happens?<br/>(e.g. AuthorizationBehavior denies)"}
    ShortCircuit -->|"yes"| NoTx["No transaction was ever opened -<br/>nothing to roll back"]
    ShortCircuit -->|"no"| Open["Transaction opens on first write<br/>(or eagerly, for ITransactionalRequest)"]
    Open --> Outcome{"Handler's outcome?"}
    Outcome -->|"Success"| Commit["Commit"]
    Outcome -->|"Failure case<br/>(NotFound, Forbidden, Conflict, ValidationError, ...)<br/>or IUnitOfWork.Cancel"| Rollback["Rollback"]

    classDef callout fill:#fff3cd,color:#664d03,stroke:#997404,stroke-width:2px;
    class NoTx callout
```

```csharp
public record UpdateProduct(int Id, string Name, int Version)
    : ICommand<OperationOutcome<Success, NotFound, Conflict>>,
      ITransactionalRequest;

public class UpdateProductHandler
    : ICommandHandler<UpdateProduct, OperationOutcome<Success, NotFound, Conflict>>
{
    private readonly Lazy<AppDbContext> lazyContext;

    public async Task<OperationOutcome<Success, NotFound, Conflict>> Handle(
        UpdateProduct command, CancellationToken ct)
    {
        var context = this.lazyContext.Value;
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == command.Id, ct);

        if (product == null)
            return new NotFound($"Product {command.Id}");  // Rolls back transaction

        if (product.Version != command.Version)
            return new Conflict("Concurrency conflict");  // Rolls back transaction

        product.Name = command.Name;
        await context.SaveChangesAsync(ct);
        return new Success();  // Commits transaction
    }
}
```

## Where to register the behavior, relative to pipeline stages

Because the transaction only covers what runs *after* `UnitOfWorkBehavior` begins it, a `PipelineStageBehavior`-hosted stage that performs a check-then-act read needs `UnitOfWorkBehavior` registered *outside* the stage host, not innermost the way the library's recommended chain places it:

```csharp
// The duplicate-title stage's read and the handler's insert below it share one transaction only because
// the unit-of-work behavior wraps the stage host, not the other way around.
builder.Pipeline
    .AddLoggingBehavior()
    .AddBehavior(typeof(AppUnitOfWorkBehavior<,>))
    .AddStage<DuplicateTitleGuardStage>()
    .AddStageHost();
```

The runnable sample under [`samples/Interlocutor.Sample.WebApi`](../samples/Interlocutor.Sample.WebApi/Program.cs) composes its pipeline exactly this way, for exactly this reason - see its `DuplicateTitleGuardStage`.

## Nested Scope Behavior

If you dispatch from within a command handler (rare, but possible), each dispatch still shares the parent's scoped session:

```csharp
public class ComplexOrderHandler : ICommandHandler<CreateComplexOrder, OperationOutcome<int>>
{
    private readonly Lazy<AppDbContext> lazyContext;
    private readonly ISender mediator;

    public async Task<OperationOutcome<int>> Handle(
        CreateComplexOrder command, CancellationToken ct)
    {
        var context = this.lazyContext.Value;

        // Create the order
        var order = new Order { /* ... */ };
        context.Orders.Add(order);

        // Dispatch a nested command
        var inventoryResult = await this.mediator.Send(
            new ReserveInventory(order.Id, command.Items),
            ct
        );

        if (!inventoryResult.IsSuccess)
            return inventoryResult.Failure;  // Same transaction rolls back

        await context.SaveChangesAsync(ct);
        return order.Id;  // Same transaction commits
    }
}

public record ReserveInventory(int OrderId, List<OrderItem> Items)
    : ICommand<OperationOutcome<Success, InsufficientStock>>;

// This command runs inside the parent's transaction, sharing the same DbContext - it doesn't need its
// own ITransactionalRequest marker; the parent dispatch already opened the transaction.
```

## Transactions with Notifications

When a command publishes a notification, the notification's handlers run **after** the command's transaction commits:

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrder, OperationOutcome<int>>
{
    private readonly Lazy<AppDbContext> lazyContext;
    private readonly IPublisher publisher;

    public async Task<OperationOutcome<int>> Handle(
        CreateOrder command, CancellationToken ct)
    {
        var context = this.lazyContext.Value;
        var order = new Order { /* ... */ };
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);

        // At this point, the transaction commits (UnitOfWorkBehavior)
        // Then notification handlers run outside the transaction
        await this.publisher.Publish(new OrderCreated(order.Id, /* ... */), ct);

        return order.Id;
    }
}
```

If a notification handler fails, it **does not roll back the command**. The command already committed. This is correct for the outbox pattern - you want the command to succeed even if downstream handlers fail.

If you need to guarantee command + side effect atomicity, use the outbox pattern: publish events *inside* the transaction (as a persisted `Outbox` table row), then process them asynchronously in a separate transaction.
