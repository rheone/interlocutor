# Dependency Injection: Microsoft.Extensions.DependencyInjection

`Interlocutor.DependencyInjection.Microsoft` translates the container-neutral registration model from [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md) into `IServiceCollection` registrations, and adds the Microsoft DI-specific surface: lifetime/open-generic translation, root-scope detection, and the persistence registration extensions. This doc is registration, injection, and validation examples for this package specifically - see the Abstractions doc for the registration model itself, and [Builder & Registration](builder-and-registration.md) for the full `InterlocutorBuilder` API.

## Entry Point

```csharp
public static class InterlocutorServiceCollectionExtensions
{
    public static IServiceCollection AddInterlocutor(
        this IServiceCollection services,
        Action<InterlocutorBuilder> configure);
}
```

```csharp
var builder = WebApplication.CreateBuilder();
builder.Services.AddInterlocutor(interlocutor =>
{
    interlocutor.ScanAssembly(typeof(Program).Assembly);
    interlocutor.Pipeline.UseRecommendedChain();
});

var app = builder.Build();
```

## Registration Examples

### Scanning an assembly

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    // Scoped by default; pin a different lifetime per assembly if needed.
    builder.ScanAssembly(typeof(SomeSingletonHandler).Assembly, RegistrationLifetime.Singleton);
});
```

### Registering one handler explicitly

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.AddHandler(typeof(CreateOrderHandler));
    builder.AddHandler(typeof(GenericAuditHandler<>), RegistrationLifetime.Singleton); // open generic
});
```

### Registering by predicate

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.AddTypeMatch(
        t => t.Namespace == "MyApp.Audit",
        TypeMatchTarget.Handler,
        except: t => t == typeof(LegacyAuditHandler),
        name: "audit-handlers");
});
```

### Composing the pipeline

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    builder.Pipeline.AddLoggingBehavior();
    builder.Pipeline.AddValidationBehavior();  // no-op if Interlocutor.FluentValidation isn't referenced
    builder.Pipeline.AddUnitOfWorkBehavior(); // no-op if neither store package is referenced

    // Or the sugar form - calls exactly the three above, in this order:
    // builder.Pipeline.UseRecommendedChain();
    // Auditing/Authorization are opt-in, not part of this default - add them explicitly if you want them:
    // builder.Pipeline.AddAuditingBehavior();
    // builder.Pipeline.AddAuthorizationBehavior();
});
```

See [Builder & Registration](builder-and-registration.md) and [Pipeline & Behavior Composition](pipeline-and-behaviors.md) for the full set of registration/composition forms - the ones above are what's available regardless of which container package is referenced. What follows is specific to this package.

### Lifetime Translation

| `RegistrationLifetime` | `ServiceLifetime` |
| :-- | :-- |
| `Singleton` | `Singleton` |
| `Scoped` | `Scoped` |
| `Transient` | `Transient` |

Every registration becomes a `ServiceDescriptor` - `services.Add(...)` for `Handler`/`Behavior`/`Stage`/`Dispatcher`, `services.TryAdd(...)` for `Default`.

### Open Generic Resolution

`Pipeline.AddBehavior` always records the open `IPipelineBehavior<TRequest, TResponse>` as the service type, regardless of whether the implementation type is itself open or closed. Microsoft.Extensions.DependencyInjection requires an open generic *service* type to be paired with an open generic *implementation* - a closed implementation registered directly against `IPipelineBehavior<,>` throws at `BuildServiceProvider()` time. `AddInterlocutor` re-pairs a closed implementation with the specific closed `IPipelineBehavior<,>` interface it actually implements, so this never reaches the container in the invalid shape.

> [!WARNING]
> If a behavior implementation and the service type it's registered against are *both* open generics with a different number of type parameters - for example, EF Core's arity-3 `UnitOfWorkBehavior<TRequest, TResponse, TContext>` registered directly against the arity-2 `IPipelineBehavior<TRequest, TResponse>` - `AddInterlocutor` throws `InvalidOperationException` immediately, with a message naming the fix: declare a subclass that closes the extra type parameter and leaves the rest open, then register that subclass via `Pipeline.AddBehavior(typeof(...))` instead.
>
> ```csharp
> class MyUnitOfWorkBehavior<TRequest, TResponse>
>     : UnitOfWorkBehavior<TRequest, TResponse, MyDbContext> { }
>
> builder.Pipeline.AddBehavior(typeof(MyUnitOfWorkBehavior<,>));
> ```

### Persistence Registration Extensions

```csharp
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInterlocutorUnitOfWork<TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, IUnitOfWork;

    public static IServiceCollection AddInterlocutorRepository<TEntity, TKey, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory)
        where TImplementation : class, IDomainRepository<TEntity, TKey>;
}
```

Both register scoped, and both take a factory rather than a bare implementation type - an EF Core or NHibernate repository's constructor needs a key-selector expression (`e => e.Id`) no container can synthesize. Neither is called by `AddInterlocutor` itself - the persistence abstraction is entirely opt-in:

```csharp
services.AddInterlocutorUnitOfWork(sp =>
    new EntityFrameworkUnitOfWork<AppDbContext>(new Lazy<AppDbContext>(sp.GetRequiredService<AppDbContext>)));

services.AddInterlocutorRepository<Order, int, EntityFrameworkRepository<Order, int, AppDbContext>>(sp =>
    new EntityFrameworkRepository<Order, int, AppDbContext>(
        new Lazy<AppDbContext>(sp.GetRequiredService<AppDbContext>),
        order => order.Id));
```

See [Persistence Abstraction](persistence-abstraction.md) and [Persistence Implementations](persistence-implementations.md) for the full `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` surface these register.

## Injection Examples

### Constructor injection in an ASP.NET Core controller

The framework creates one DI scope per HTTP request, so a controller constructor-injecting `ISender` (or `IMediator`) always resolves inside a valid scope:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUser(id), ct);
        return result.ToActionResult();
    }
}
```

### Minimal API

```csharp
app.MapGet("/users/{id:int}", async (int id, ISender mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetUser(id), ct);
    return result.ToActionResult();
});
```

### Background service - explicit scope required

Unlike a controller, a `BackgroundService` has no framework-created request scope, so it must create one explicitly before resolving the mediator:

```csharp
public sealed class OrderPollingService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessPendingOrders(), stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

> [!CAUTION]
> Resolving `IMediator`/`ISender`/`IPublisher` directly from the root `IServiceProvider` (skipping `CreateScope()`) throws - see [Root-Scope Detection](#root-scope-detection) below and [Mediator, Lifetimes & Scope § Root-Scope Exception & Guard](mediator-lifetimes-and-scope.md#root-scope-exception--guard) for the full rationale.

### Injecting a repository/unit of work in a handler

```csharp
public sealed class CreateOrderHandler(IDomainRepository<Order, int> orders, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrder, Outcome<int, ValidationError>>,
      ITransactionalRequest
{
    public async Task<Outcome<int, ValidationError>> Handle(CreateOrder command, CancellationToken ct)
    {
        var order = new Order(command.CustomerId, command.Items);
        await orders.InsertAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct); // UnitOfWorkBehavior still owns commit/rollback
        return order.Id;
    }
}
```

## Validation Examples

### `ValidateOnBuild()` - at `AddInterlocutor()` time

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.ValidateOnBuild();
});
// Throws InterlocutorRegistrationValidationException synchronously from
// AddInterlocutor() if any request type has no handler, before services is
// mutated at all.
```

### `ValidateHandlerRegistrations` - after the provider is built

```csharp
var provider = services.BuildServiceProvider();

// Opt in explicitly - AddInterlocutor above did not do this for you.
provider.ValidateHandlerRegistrations(typeof(Program).Assembly);
// Throws InvalidOperationException naming every missing handler, in one message.
```

```csharp
var app = builder.Build();

// Same call, against the built WebApplication's provider.
app.Services.ValidateHandlerRegistrations(typeof(Program).Assembly);
```

### Inspecting what got registered

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    var report = builder.DescribeRegistrations();
    Console.WriteLine(report.Format());
});
```

### Registration Report Logging

If `ILoggerFactory` is already registered in `services` before `AddInterlocutor` runs, the formatted `RegistrationReport` is logged automatically at `AddInterlocutor()` time, under the `"Interlocutor"` category - additive, never required, and silently skipped if resolving a logger fails for any reason:

```csharp
services.AddLogging(logging => logging.AddConsole());

// Registration report is logged automatically once this call runs, since
// ILoggerFactory is already registered above.
services.AddInterlocutor(builder => builder.ScanAssembly(typeof(Program).Assembly));
```

## Root-Scope Detection

`IMediator` is registered as a scoped `RootScopeGuardedMediator` wrapping the real `Mediator`, so a resolve from the application's root container throws instead of silently handing back a mediator whose scoped dependencies (a `DbContext`, for instance) have nowhere to resolve from:

```csharp
// ❌ Throws - resolved directly from the root provider, no scope created
var mediator = app.Services.GetRequiredService<IMediator>();

// ✅ Correct
using var scope = app.Services.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
```

The detection mechanism:

| Type | Registered as | Role |
| :-- | :-- | :-- |
| `RootServiceProviderMarker` | Singleton | Captures the true root `IServiceProvider` via constructor injection. Microsoft.Extensions.DependencyInjection resolves a singleton's dependencies against its own internally-tracked root scope regardless of which scope first triggered construction, so this is a stable point of comparison even if the marker is built lazily from inside a request scope. |
| `MicrosoftRootScopeDetector` | Scoped, implements `IRootScopeDetector` | Compares the ambient `IServiceProvider` it was constructed with against `RootServiceProviderMarker.RootProvider`. Reference equality means this instance was resolved directly from the root provider, with no intervening `IServiceScopeFactory.CreateScope()` call. |

Both are re-registered fresh every scope alongside the mediator itself, so the comparison is recomputed per dispatch rather than cached once.

## See Also

- [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md) - the container-neutral registration model, validation mechanisms in depth, and the Microsoft DI vs. Autofac comparison.
- [Dependency Injection: Autofac](dependency-injection-autofac.md) - the equivalent surface for Autofac.
- [Builder & Registration](builder-and-registration.md) - the full `InterlocutorBuilder` how-to guide.
- [Mediator, Lifetimes, Scope & Ambient Context](mediator-lifetimes-and-scope.md) - dispatching correctly from web/background contexts, in depth.
- [Persistence Abstraction](persistence-abstraction.md) / [Persistence Implementations](persistence-implementations.md) - the `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` surface the persistence extensions register.
