# Builder & Registration

How to register Interlocutor with dependency injection. Handlers can be discovered two ways - by scanning assemblies at runtime (`ScanAssembly` and friends), or additively by consuming compile-time source-generator output (`AddGeneratedRegistrations`) - and the pipeline is empty until you explicitly compose it; nothing runs by default.

## The Builder

`InterlocutorBuilder` is the single configuration entry point, container-agnostic. You pass it the same builder delegate to both Microsoft.Extensions.DependencyInjection and Autofac, unchanged:

```csharp
// Microsoft.Extensions.DependencyInjection
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.UseRecommendedChain();
});

// Autofac - same builder delegate
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.UseRecommendedChain();
});
```

### Migration Note: No Legacy Overloads

This library has **no legacy registration API**. There are no bare `AddInterlocutor()` overloads with defaults, no `options` pattern, no separate `ScanAssembly(Assembly[])` method. All configuration flows through the builder delegate.

If you're coming from MediatR or an earlier Interlocutor prototype, the entry point is different:

```csharp
// ❌ Not supported - builder delegate is required
services.AddInterlocutor();

// ❌ Not supported - use the builder instead
services.AddInterlocutor<Program>();

// ✅ Correct - builder is always required
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
});
```

## Assembly Scanning

Handlers and notifications are discovered by scanning one or more assemblies at runtime - this is a reflection-based scan (`assembly.GetTypes()`), not the source generator:

```csharp
services.AddInterlocutor(builder =>
{
    // Scan a single assembly
    builder.ScanAssembly(typeof(Program).Assembly);

    // Scan the entry assembly (shorthand)
    builder.ScanEntryAssembly();

    // Scan an assembly containing a specific marker type
    builder.ScanAssemblyContaining<UserService>();

    // Scan multiple assemblies
    builder.ScanAssembly(typeof(MyDomainModel).Assembly);
    builder.ScanAssembly(typeof(MyApplicationService).Assembly);
});
```

Each scanned assembly is inspected independently - scanning one assembly does not automatically pull in handlers from assemblies it references. If your handlers live across several assemblies, scan each one explicitly (as above), or list several with `ScanAssemblies(...)`.

Runtime scanning looks for:

- `IQueryHandler<TQuery, TResponse>` implementations → registered as scoped
- `ICommandHandler<TCommand, TResponse>` implementations → registered as scoped
- `ICommandHandler<TCommand>` (void) implementations → registered as scoped
- `INotificationHandler<TNotification>` implementations → registered as scoped

`IPipelineStage<TRequest, TResponse>` registration is separate - stages are registered explicitly via `InterlocutorBuilder.AddStage`, not discovered by scanning.

## Pipeline Behaviors

Add behaviors to the builder, in the order you want them to execute (outermost to innermost):

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    // Explicitly compose your pipeline
    builder.AddBehavior(typeof(LoggingBehavior<,>));
    builder.AddBehavior(typeof(AuditingBehavior<,>));
    builder.AddBehavior(typeof(ValidationBehavior<,>));
    builder.AddBehavior(typeof(TransactionBehavior<,>));

    // Or use the documented recommended chain as sugar
    // builder.UseRecommendedChain();
});
```

See [pipeline-and-behaviors.md](pipeline-and-behaviors.md) for the full rationale of the recommended chain and how to compose your own.

> [!NOTE]
> Internally, `InterlocutorBuilder`'s pipeline methods (`AddBehavior`, the named `Add*Behavior` methods, `UseRecommendedChain`, `AddStage`/`AddStageHost`) delegate to a dedicated `InterlocutorPipelineBuilder` that holds the actual behavior/stage state, keeping pipeline composition a self-contained concern separate from assembly scanning and infrastructure overrides. The public surface shown above is unchanged - every call above still compiles and behaves exactly as written.

## Dependency Defaults

Some behaviors need service registrations of their own (an `IAuditSink`, an `IPolicyEvaluator`, an `IRequestContextAccessor`). The named `Add*Behavior` methods try-add a reasonable default for whatever that behavior depends on - your own registration, made before the builder is applied, always wins:

```csharp
// ✅ Your registration wins - the behavior's own try-add default never overrides it
services.AddSingleton<IAuditSink, MyAuditSink>();
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.AddAuditingBehavior();
    // MyAuditSink is used - AddAuditingBehavior's own default was never applied
});
```

If you need a dependency's registration to win regardless of call order - overriding even a registration made after the builder ran - use the matching `Override*` method instead of a plain service registration:

```csharp
services.AddSingleton<IAuditSink, MyAuditSink>();
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.AddAuditingBehavior();
    builder.OverrideAuditSink<OtherAuditSink>();  // This one wins, regardless of order
});
```

`OverrideAuditSink<TSink>()`, `OverridePolicyEvaluator<TEvaluator>()`, `OverrideRequestContextAccessor<TAccessor>()`, and `OverrideOutcomeDescriber<TDescriber>()` are generic methods, each constrained to the interface they override - there is no settable `Default*`/`Override*` property on the builder.

## Cross-Assembly Aggregation

> [!NOTE]
> **TBD:** Automatic cross-assembly aggregation - scanning one entry-point assembly and having handlers from every referenced assembly discovered transitively - is not implemented yet. Each assembly that contains handlers needs to reach the builder explicitly today (see below). Write this section from the actual implemented behavior once that lands, not from the design intent.

Today, if handlers live in more than one assembly, scan each of them:

```csharp
// Program.cs (MyApp.Application), handlers also live in MyApp.Domain
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.ScanAssembly(typeof(MyApp.Domain.SomeHandler).Assembly);
    builder.UseRecommendedChain();
});
```

If you're using the compile-time generator instead of runtime scanning, each assembly's own generated registrations module is added the same way, once per assembly:

```csharp
services.AddInterlocutor(builder =>
{
    builder.AddGeneratedRegistrations(Interlocutor.Generated.InterlocutorGeneratedRegistrations.Registrations);
    // A referenced assembly's own generated module is added the same way, explicitly.
});
```

## Startup Validation

`AddInterlocutor` itself does not validate that every discovered request type has a registered handler - it only accumulates and translates registrations, both for runtime scanning and for generated registrations. Two separate mechanisms exist for catching a missing handler, neither of them automatic:

- **A build-time diagnostic from the source generator** (`INTERLOC001`) fires as a compiler warning when a request type has no matching handler *in the same compilation* - default severity `Warning`, escalatable to a build error via `.editorconfig` (`dotnet_diagnostic.INTERLOC001.severity = error`). This only sees what the compiler sees; it can't know about handlers registered through runtime scanning of an assembly outside the current compilation.
- **An explicit, opt-in runtime check**, made once your container is built: `IServiceProvider.ValidateHandlerRegistrations(params Assembly[])`. It reflects over the given assemblies, derives each request type's expected handler contract, and throws `InvalidOperationException` - naming every missing registration in one message - if any are absent.

```csharp
var provider = services.BuildServiceProvider();

// Opt in explicitly - AddInterlocutor above did not do this for you.
provider.ValidateHandlerRegistrations(typeof(Program).Assembly);
```

Call this once at startup (after building your provider/container) if you want missing-handler detection before the first request, rather than discovering it the first time a request with no handler is dispatched.

## Microsoft.Extensions.DependencyInjection

Entry point: `IServiceCollection.AddInterlocutor(Action<InterlocutorBuilder>)`

```csharp
var builder = WebApplication.CreateBuilder();
builder.Services.AddInterlocutor(interlocutor =>
{
    interlocutor.ScanAssembly(typeof(Program).Assembly);
    interlocutor.UseRecommendedChain();
});

var app = builder.Build();

// Optional - see Startup Validation above. Missing handlers are not caught automatically.
app.Services.ValidateHandlerRegistrations(typeof(Program).Assembly);
```

## Autofac

Entry point: `ContainerBuilder.AddInterlocutor(Action<InterlocutorBuilder>)`

```csharp
var containerBuilder = new ContainerBuilder();

// Identical builder delegate, same method signature
containerBuilder.AddInterlocutor(interlocutor =>
{
    interlocutor.ScanAssembly(typeof(Program).Assembly);
    interlocutor.UseRecommendedChain();
});

var container = containerBuilder.Build();

// Optional - see Startup Validation above. Missing handlers are not caught automatically.
container.Resolve<IServiceProvider>().ValidateHandlerRegistrations(typeof(Program).Assembly);
```

The builder produces container-neutral `InterlocutorRegistration` records. Both Microsoft DI and Autofac extensions translate those into their respective service descriptors / module registrations.

## Container Lifetime: Mediator is Scoped

The mediator (`IMediator`, `ISender`, `IPublisher`) is **registered as scoped** and is **not configurable**:

- In a web context, one scope per HTTP request → one mediator instance per request.
- In a background service, you create a scope explicitly before dispatching.

See [mediator-lifetimes-and-scope.md](mediator-lifetimes-and-scope.md) for the rationale and how to dispatch correctly in background services.

## Verifying Your Registration

Missing handlers are **not** caught automatically at container-build time (see Startup Validation above) - either call `ValidateHandlerRegistrations` explicitly, or verify by resolving:

```csharp
using var scope = serviceProvider.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

// If a handler is missing, GetRequiredService throws when you actually dispatch it
var result = await mediator.Send(new GetUser(1), cancellationToken);
```

`ValidateHandlerRegistrations` is the cheaper option if you want to know about *every* missing handler at once, before the first request - resolving `IMediator` and dispatching only surfaces one missing handler at a time, on first use.
