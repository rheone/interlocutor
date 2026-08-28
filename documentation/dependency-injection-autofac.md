# Dependency Injection: Autofac

`Interlocutor.DependencyInjection.Autofac` translates the container-neutral registration model from [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md) into `ContainerBuilder` registrations, and adds the Autofac-specific surface: lifetime/open-generic translation, the `IServiceProvider` bridge, root-scope detection, and the persistence registration extensions. This doc is registration, injection, and validation examples for this package specifically - see the Abstractions doc for the registration model itself, and [Builder & Registration](builder-and-registration.md) for the full `InterlocutorBuilder` API.

## Entry Point

```csharp
public static class InterlocutorAutofacExtensions
{
    public static ContainerBuilder AddInterlocutor(
        this ContainerBuilder builder,
        Action<InterlocutorBuilder> configure);
}
```

```csharp
var containerBuilder = new ContainerBuilder();

// Identical builder delegate, same method signature as Microsoft DI's AddInterlocutor.
containerBuilder.AddInterlocutor(interlocutor =>
{
    interlocutor.ScanAssembly(typeof(Program).Assembly);
    interlocutor.Pipeline.UseRecommendedChain();
});

var container = containerBuilder.Build();
```

## Registration Examples

### Scanning an assembly

```csharp
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    // Scoped by default; pin a different lifetime per assembly if needed.
    builder.ScanAssembly(typeof(SomeSingletonHandler).Assembly, RegistrationLifetime.Singleton);
});
```

### Registering one handler explicitly

```csharp
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.AddHandler(typeof(CreateOrderHandler));
    builder.AddHandler(typeof(GenericAuditHandler<>), RegistrationLifetime.Singleton); // open generic
});
```

### Registering by predicate

```csharp
containerBuilder.AddInterlocutor(builder =>
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
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    builder.Pipeline.AddLoggingBehavior();
    builder.Pipeline.AddAuditingBehavior();
    builder.Pipeline.AddValidationBehavior();  // no-op if Interlocutor.FluentValidation isn't referenced
    builder.Pipeline.AddUnitOfWorkBehavior(); // no-op if neither store package is referenced

    // Or the sugar form - calls exactly the five above, in this order:
    // builder.Pipeline.UseRecommendedChain();
});
```

Per-request-type pipeline overrides (`Pipeline.OverridePipelineFor`) work identically under Autofac as under Microsoft DI: the same declaration API, the same resolved pipeline for a matched request, and the same exclusivity guarantee against marker-constrained behaviors it doesn't name. Declaring an override registers its behaviors through the same container-neutral translation every other behavior goes through - there is nothing Autofac-specific to configure. See [Per-Request-Type Pipeline Overrides](pipeline-and-behaviors.md#per-request-type-pipeline-overrides) for the full walkthrough.

### Registering from an Autofac module

`AddInterlocutor` works identically inside a module's `Load` override - there is no module-specific variant:

```csharp
public sealed class MediatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.AddInterlocutor(interlocutor =>
        {
            interlocutor.ScanAssembly(typeof(MediatorModule).Assembly);
            interlocutor.Pipeline.UseRecommendedChain();
        });
    }
}

var containerBuilder = new ContainerBuilder();
containerBuilder.RegisterModule<MediatorModule>();
var container = containerBuilder.Build();
```

See [Builder & Registration](builder-and-registration.md) and [Pipeline & Behavior Composition](pipeline-and-behaviors.md) for the full set of registration/composition forms - the ones above are what's available regardless of which container package is referenced. What follows is specific to this package.

### Lifetime Translation

| `RegistrationLifetime` | Autofac scope method |
| :-- | :-- |
| `Singleton` | `SingleInstance()` |
| `Scoped` | `InstancePerLifetimeScope()` |
| `Transient` | `InstancePerDependency()` |

Handlers discovered by scanning are registered `InstancePerLifetimeScope` - unlike Microsoft DI, where the equivalent lifetime is called `Scoped`, but the semantics (one instance per DI scope) are identical.

### Open Generic Resolution

Unlike Microsoft DI, Autofac has no problem pairing an *open* generic implementation with an open generic service via `RegisterGeneric(...).As(...)`. The reverse shape - a *closed* implementation paired directly with an *open* service type via `RegisterType(...).As(...)` - throws `ArgumentException` at `ContainerBuilder.Build()` time instead. `AddInterlocutor` applies the identical re-pairing Microsoft DI's package does: a closed implementation registered against the open `IPipelineBehavior<TRequest, TResponse>` is re-paired with the specific closed interface it implements before `RegisterType` is called.

### Try-Add Defaults

Infrastructure defaults are registered with `.IfNotRegistered(serviceType)` - Autofac's try-add equivalent - so a consumer's own registration made before `AddInterlocutor` runs is left untouched, matching Microsoft DI's `TryAdd` semantics:

```csharp
containerBuilder.RegisterType<MyAuditSink>().As<IAuditSink>().InstancePerLifetimeScope();
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.Pipeline.AddAuditingBehavior();
    // MyAuditSink is used - AddAuditingBehavior's own IfNotRegistered default never applies.
});
```

### `IServiceProvider` Bridge

The mediator resolves handlers through `IServiceProvider`, not Autofac's own `IComponentContext`, so `AddInterlocutor` registers an `AutofacServiceProvider` (from `Autofac.Extensions.DependencyInjection`) wrapping the current `ILifetimeScope`, marked `.ExternallyOwned()` so Autofac does not attempt to dispose it independently of the scope it wraps. This registration is automatic - no consumer action needed - but it's why `IServiceProvider` resolves successfully from an Autofac-built container even though nothing registered it explicitly by hand.

### Persistence Registration Extensions

```csharp
public static class PersistenceContainerBuilderExtensions
{
    public static ContainerBuilder AddInterlocutorUnitOfWork<TImplementation>(
        this ContainerBuilder builder,
        Func<IComponentContext, TImplementation> implementationFactory)
        where TImplementation : notnull, IUnitOfWork;

    public static ContainerBuilder AddInterlocutorRepository<TEntity, TKey, TImplementation>(
        this ContainerBuilder builder,
        Func<IComponentContext, TImplementation> implementationFactory)
        where TImplementation : notnull, IDomainRepository<TEntity, TKey>;
}
```

Both register `InstancePerLifetimeScope`, over Autofac's own `IComponentContext` rather than `IServiceProvider`. Neither is called by `AddInterlocutor` itself - the persistence abstraction is entirely opt-in:

```csharp
containerBuilder.AddInterlocutorUnitOfWork(c =>
    new EntityFrameworkUnitOfWork<AppDbContext>(c.Resolve<Lazy<AppDbContext>>()));

containerBuilder.AddInterlocutorRepository<Order, int, EntityFrameworkRepository<Order, int, AppDbContext>>(c =>
    new EntityFrameworkRepository<Order, int, AppDbContext>(c.Resolve<Lazy<AppDbContext>>(), order => order.Id));
```

> [!TIP]
> Autofac resolves `Lazy<T>` natively for any registered `T` - there is no need for a separate `Lazy<AppDbContext>` registration. Just be sure not to capture the `IComponentContext` parameter itself inside a lambda for later use; resolve everything the lambda needs from it before the lambda returns.

See [Persistence Abstraction](persistence-abstraction.md) and [Persistence Implementations](persistence-implementations.md) for the full `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` surface these register.

## Injection Examples

### Resolving directly from a built container

```csharp
var container = containerBuilder.Build();

// ❌ Throws - resolved directly from the root container, no lifetime scope created
var mediator = container.Resolve<IMediator>();

// ✅ Correct
await using var scope = container.BeginLifetimeScope();
var mediator = scope.Resolve<IMediator>();
var result = await mediator.Send(new GetUser(42));
```

### Constructor injection into an application service

Autofac resolves constructor dependencies the same way regardless of what triggered the resolution, so a service registered into the container can constructor-inject `ISender` exactly like a Microsoft DI-hosted controller does:

```csharp
public sealed class OrderApiService(ISender mediator)
{
    public Task<OperationOutcome<OrderDto, NotFound>> GetOrder(int id, CancellationToken ct) =>
        mediator.Send(new GetOrder(id), ct);
}

containerBuilder.RegisterType<OrderApiService>().AsSelf().InstancePerLifetimeScope();
```

### Background/worker loop - explicit lifetime scope required

Exactly like Microsoft DI's `BackgroundService`, a long-running Autofac-hosted loop must create a lifetime scope explicitly per unit of work - there is no ambient request scope to lean on:

```csharp
public sealed class OrderPollingService(ILifetimeScope rootScope)
{
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = rootScope.BeginLifetimeScope();
            var mediator = scope.Resolve<IMediator>();
            await mediator.Send(new ProcessPendingOrders(), stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

> [!CAUTION]
> Resolving `IMediator`/`ISender`/`IPublisher` directly from the root container (skipping `BeginLifetimeScope()`) throws - see [Root-Scope Detection](#root-scope-detection) below and [Mediator, Lifetimes & Scope § Root-Scope Exception & Guard](mediator-lifetimes-and-scope.md#root-scope-exception--guard) for the full rationale.

### Injecting a repository/unit of work in a handler

Identical to the Microsoft DI case - a handler constructor-injects the store-agnostic interfaces, unaware of which container resolved it:

```csharp
public sealed class CreateOrderHandler(IDomainRepository<Order, int> orders, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrder, OperationOutcome<int, ValidationError>>,
      ITransactionalRequest
{
    public async Task<OperationOutcome<int, ValidationError>> Handle(CreateOrder command, CancellationToken ct)
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
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.ValidateOnBuild();
});
// Throws InterlocutorRegistrationValidationException synchronously from
// AddInterlocutor() if any request type has no handler, before builder is
// mutated at all.
```

### `ValidateHandlerRegistrations` - after the container is built

Unlike Microsoft DI, Autofac has no native `IServiceProvider` to call this extension on directly - resolve the bridged one first:

```csharp
var container = containerBuilder.Build();

// Opt in explicitly - AddInterlocutor above did not do this for you.
container.Resolve<IServiceProvider>().ValidateHandlerRegistrations(typeof(Program).Assembly);
// Throws InvalidOperationException naming every missing handler, in one message.
```

### Inspecting what got registered

```csharp
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    var report = builder.DescribeRegistrations();
    Console.WriteLine(report.Format());
});
```

### Registration Report Logging

Unlike Microsoft DI, Autofac's registration order is unordered relative to the `AddInterlocutor` call, so whether `ILoggerFactory` ends up registered can't be checked synchronously. The formatted `RegistrationReport` is instead logged from a `RegisterBuildCallback`, deferred until the container finishes building - skipped silently if no `ILoggerFactory` is resolvable from the finished container:

```csharp
containerBuilder.Register(c => LoggerFactory.Create(b => b.AddConsole())).As<ILoggerFactory>().SingleInstance();

// Registration report is logged once Build() runs below, since ILoggerFactory
// is resolvable from the finished container by then - registration order
// relative to AddInterlocutor doesn't matter here.
containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(Program).Assembly));
var container = containerBuilder.Build();
```

## Root-Scope Detection

`IMediator` is registered `InstancePerLifetimeScope` as a `RootScopeGuardedMediator` wrapping the real `Mediator`, so a resolve from the root container throws instead of silently handing back a mediator whose scoped dependencies have nowhere to resolve from:

```csharp
// ❌ Throws - resolved directly from the root container
var mediator = container.Resolve<IMediator>();

// ✅ Correct
await using var scope = container.BeginLifetimeScope();
var mediator = scope.Resolve<IMediator>();
```

The detection mechanism: Autofac auto-injects `ILifetimeScope` as the ambient currently-resolving scope with no explicit registration needed. The root `IContainer` `ContainerBuilder.Build()` produces is itself an `ILifetimeScope` whose `Tag` is `LifetimeScope.RootTag`; any scope created via `BeginLifetimeScope()` - directly under the root or nested further - carries a different tag.

| Type | Registered as | Role |
| :-- | :-- | :-- |
| `AutofacRootScopeDetector` | `InstancePerLifetimeScope`, implements `IRootScopeDetector` | Compares the ambient `ILifetimeScope.Tag` it was constructed with against `LifetimeScope.RootTag`. Equal means this instance was resolved directly from the root, regardless of how many other child scopes exist elsewhere in the application. |

## See Also

- [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md) - the container-neutral registration model, validation mechanisms in depth, and the Microsoft DI vs. Autofac comparison.
- [Dependency Injection: Microsoft.Extensions.DependencyInjection](dependency-injection-microsoft.md) - the equivalent surface for Microsoft DI.
- [Builder & Registration](builder-and-registration.md) - the full `InterlocutorBuilder` how-to guide.
- [Mediator, Lifetimes, Scope & Ambient Context](mediator-lifetimes-and-scope.md) - dispatching correctly from web/background contexts, in depth.
- [Persistence Abstraction](persistence-abstraction.md) / [Persistence Implementations](persistence-implementations.md) - the `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` surface the persistence extensions register.
