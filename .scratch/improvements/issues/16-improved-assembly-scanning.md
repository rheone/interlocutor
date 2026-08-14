## Labels

ready-for-agent

## What to build

Improve the assembly-scanning registration experience. Currently, consumers must pass assemblies explicitly:

```csharp
services.AddInterlocutor(typeof(GetOrderQueryHandler).Assembly);
```

The MediatR equivalent (`AddMediatR(config)` with assembly scanning) is more ergonomic. Add similar convenience overloads:

```csharp
// Existing (works but verbose for single-assembly scenarios)
public static IServiceCollection AddInterlocutor(this IServiceCollection services, params Assembly[] assembliesToScan)

// New convenience (most common case — single marker type)
public static IServiceCollection AddInterlocutor(this IServiceCollection services, Type markerType)
    => services.AddInterlocutor(markerType.Assembly);

// New convenience (auto-scan entry assembly)
public static IServiceCollection AddInterlocutorFromEntryAssembly(this IServiceCollection services)
    => services.AddInterlocutor(Assembly.GetEntryAssembly()!);
```

Also consider: should pipeline behaviors (`IPipelineBehavior<TRequest,TResponse>`) also be auto-registered? Currently only handlers are scanned. Behaviors are registered manually. This is fine for now — ticket scope is limited to handler scanning convenience.

Same pattern should be applied to `Interlocutor.DependencyInjection.Autofac.AddInterlocutor()`.

## Acceptance criteria

- [ ] `AddInterlocutor(Type markerType)` overload exists for MS DI
- [ ] `AddInterlocutorFromEntryAssembly()` overload exists for MS DI
- [ ] Same overloads exist for Autofac
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
