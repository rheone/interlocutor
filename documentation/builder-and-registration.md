# Builder & Registration

How to register Interlocutor with dependency injection. Handlers can be discovered two ways - by scanning assemblies at runtime (`ScanAssembly` and friends), or additively by consuming compile-time source-generator output (`AddGeneratedRegistrations`) - and the pipeline is empty until you explicitly compose it; nothing runs by default.

## The Builder

`InterlocutorBuilder` is the single configuration entry point, container-agnostic. You pass it the same builder delegate to both Microsoft.Extensions.DependencyInjection and Autofac, unchanged:

```csharp
// Microsoft.Extensions.DependencyInjection
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.Pipeline.UseRecommendedChain();
});

// Autofac - same builder delegate
containerBuilder.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.Pipeline.UseRecommendedChain();
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

`ScanAssembly`, `ScanAssemblyContaining<TMarker>`, and `ScanEntryAssembly` each take an optional `RegistrationLifetime`, applied to every handler that assembly yields; `ScanAssemblies` has a `RegistrationLifetime` overload for the same, since a `params` array forces the lifetime to come first. All default to scoped.

```csharp
builder.ScanAssembly(typeof(MyDomainModel).Assembly, RegistrationLifetime.Singleton);
```

`IPipelineStage<TRequest, TResponse>` registration is separate - stages are registered explicitly via `InterlocutorPipelineBuilder.AddStage` (`builder.Pipeline.AddStage`), not discovered by scanning.

### Registering a Single Handler

`AddHandler` registers one handler implementation type directly, without scanning - the manual escape hatch for handlers, mirroring `AddBehavior`'s shape:

```csharp
builder.AddHandler(typeof(CreateOrderHandler));
builder.AddHandler(typeof(GenericAuditHandler<>), RegistrationLifetime.Singleton); // open generic
```

An `AddHandler` call takes precedence over a duplicate discovered by scanning in the same configuration - it replaces, rather than duplicates, the scanned registration for that (service type, implementation type) pair.

### Registering by Predicate

`AddTypeMatch` registers every candidate type found while scanning that a predicate matches, as a handler or a behavior:

```csharp
builder.ScanAssembly(typeof(Program).Assembly);
builder.AddTypeMatch(
    t => t.Namespace == "MyApp.Audit",
    TypeMatchTarget.Handler,
    except: t => t == typeof(LegacyAuditHandler),
    name: "audit-handlers"
);
```

Matching zero types is legal and silent by default; pass `require: true` to record that this rule must match at least one type - `ValidateOnBuild()` (below) is what actually enforces it. This is strictly additive to marker-interface-based behavior scoping (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest`) - both mechanisms work independently and simultaneously.

## Pipeline Behaviors

Add behaviors to the builder, in the order you want them to execute (outermost to innermost):

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);

    // Explicitly compose your pipeline
    builder.Pipeline.AddLoggingBehavior();
    builder.Pipeline.AddAuditingBehavior();
    builder.Pipeline.AddValidationBehavior();     // no-op if Interlocutor.FluentValidation isn't referenced
    builder.Pipeline.AddTransactionBehavior();    // no-op if neither store package is referenced

    // Or use the documented recommended chain as sugar - it calls exactly these five, in this order
    // builder.Pipeline.UseRecommendedChain();
});
```

See [pipeline-and-behaviors.md](pipeline-and-behaviors.md) for the full rationale of the recommended chain and how to compose your own.

> [!IMPORTANT]
> Every pipeline-composition method - `AddBehavior`, the named `Add*Behavior` methods, `UseRecommendedChain`, `AddStage`/`AddStageHost`, and the behavior-dependency `Override*` methods below other than `OverrideRequestContextAccessor` - lives on `InterlocutorPipelineBuilder`, reachable only through `InterlocutorBuilder.Pipeline`. This is a deliberate breaking change from an earlier revision where those methods lived directly on `InterlocutorBuilder`: `.Pipeline` keeps "what runs in what order" a self-contained concern, with one canonical home for its documentation, separate from `InterlocutorBuilder`'s own assembly-scanning and ambient-default responsibilities.

## Dependency Defaults

Some behaviors need service registrations of their own (an `IAuditSink`, an `IPolicyEvaluator`, an `IRequestContextAccessor`). The named `Add*Behavior` methods try-add a reasonable default for whatever that behavior depends on - your own registration, made before the builder is applied, always wins:

```csharp
// ✅ Your registration wins - the behavior's own try-add default never overrides it
services.AddSingleton<IAuditSink, MyAuditSink>();
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.Pipeline.AddAuditingBehavior();
    // MyAuditSink is used - AddAuditingBehavior's own default was never applied
});
```

If you need a dependency's registration to win regardless of call order - overriding even a registration made after the builder ran - use the matching `Override*` method instead of a plain service registration:

```csharp
services.AddSingleton<IAuditSink, MyAuditSink>();
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.Pipeline.AddAuditingBehavior();
    builder.Pipeline.OverrideAuditSink<OtherAuditSink>();  // This one wins, regardless of order
});
```

`Pipeline.OverrideAuditSink<TSink>()`, `Pipeline.OverridePolicyEvaluator<TEvaluator>()`, and `Pipeline.OverrideOutcomeDescriber<TDescriber>()` are generic methods on `InterlocutorPipelineBuilder`, each constrained to the interface they override - there is no settable `Default*`/`Override*` property. `OverrideRequestContextAccessor<TAccessor>()` is the one exception: it stays directly on `InterlocutorBuilder` itself, since `IRequestContextAccessor` is ambient infrastructure rather than a pipeline-behavior dependency.

## Cross-Assembly Aggregation

> [!NOTE]
> Aggregation is explicit, not transitive. Scanning or registering one assembly never pulls in handlers from assemblies it references - each assembly that contains handlers reaches the builder its own `ScanAssembly`/`AddGeneratedRegistrations` call. This is the permanent design, not a stopgap: it keeps "what got registered" fully explainable from the builder delegate alone, without a hidden dependency-closure walk.

If handlers live in more than one assembly, scan each of them:

```csharp
// Program.cs (MyApp.Application), handlers also live in MyApp.Domain
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    builder.ScanAssembly(typeof(MyApp.Domain.SomeHandler).Assembly);
    builder.Pipeline.UseRecommendedChain();
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

## Inspecting What Was Registered

`InterlocutorBuilder.DescribeRegistrations()` returns a structured, container-neutral `RegistrationReport` - every accumulated registration with its provenance (`Scanned`, `Explicit`, or `Matched`, plus which rule for `Matched`), every optional-package probe that found nothing (`AddValidationBehavior`/`AddTransactionBehavior` when `Interlocutor.FluentValidation`/`.EntityFrameworkCore`/`.NHibernate` aren't referenced), and, once `ValidateOnBuild()` is configured, every validation problem found. Call it at any time; `Format()`/`ToString()` render it as text:

```csharp
services.AddInterlocutor(builder =>
{
    builder.ScanAssembly(typeof(Program).Assembly);
    var report = builder.DescribeRegistrations();
    Console.WriteLine(report.Format());
});
```

If the application has already registered `ILoggerFactory` before `AddInterlocutor()` runs, both DI packages log the formatted report automatically at that point - additive, never required.

## Startup Validation

`AddInterlocutor` does not validate that every discovered request type has a registered handler unless you opt in - it otherwise only accumulates and translates registrations, both for runtime scanning and for generated registrations. Three separate mechanisms exist for catching a missing handler, none of them automatic:

- **A build-time diagnostic from the source generator** (`INTERLOC001`) fires as a compiler warning when a request type has no matching handler *in the same compilation* - default severity `Warning`, escalatable to a build error via `.editorconfig` (`dotnet_diagnostic.INTERLOC001.severity = error`). This only sees what the compiler sees; it can't know about handlers registered through runtime scanning of an assembly outside the current compilation.
- **`InterlocutorBuilder.ValidateOnBuild()`**, checked entirely from the accumulated registration list - no live container needed - the moment `AddInterlocutor()` is called:

  ```csharp
  services.AddInterlocutor(builder =>
  {
      builder.ScanAssembly(typeof(Program).Assembly);
      builder.ValidateOnBuild();
  });
  ```

  Besides an unhandled query/command type, it also catches a required `AddTypeMatch` rule that matched nothing, and two registrations of the same (service type, implementation type) pair at conflicting lifetimes. Every problem found is aggregated into one `InterlocutorRegistrationValidationException`, thrown from `AddInterlocutor()` before it mutates the container at all - carrying the same `RegistrationReport` `DescribeRegistrations()` returns, with `Problems` populated.
- **An explicit, opt-in runtime check**, made once your container is built: `IServiceProvider.ValidateHandlerRegistrations(params Assembly[])`. It reflects over the given assemblies, derives each request type's expected handler contract, and throws `InvalidOperationException` - naming every missing registration in one message - if any are absent. Unlike `ValidateOnBuild()`, this needs a live container, so it can also catch what a registration-list-only check cannot (this method's own scope is otherwise the same query/command check as `ValidateOnBuild()`).

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
    interlocutor.Pipeline.UseRecommendedChain();
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
    interlocutor.Pipeline.UseRecommendedChain();
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
