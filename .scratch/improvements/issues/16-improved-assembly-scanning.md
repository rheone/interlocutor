## Labels

ready-for-agent

## What to build

Improve the assembly-scanning registration experience. Currently, consumers must pass assemblies explicitly:

```csharp
services.AddMediance(typeof(GetOrderQueryHandler).Assembly);
```

The MediatR equivalent (`AddMediatR(config)` with assembly scanning) is more ergonomic. Add similar convenience overloads:

```csharp
// Existing (works but verbose for single-assembly scenarios)
public static IServiceCollection AddMediance(this IServiceCollection services, params Assembly[] assembliesToScan)

// New convenience (most common case — single marker type)
public static IServiceCollection AddMediance(this IServiceCollection services, Type markerType)
    => services.AddMediance(markerType.Assembly);

// New convenience (auto-scan entry assembly)
public static IServiceCollection AddMedianceFromEntryAssembly(this IServiceCollection services)
    => services.AddMediance(Assembly.GetEntryAssembly()!);
```

Also consider: should pipeline behaviors (`IPipelineBehavior<TRequest,TResponse>`) also be auto-registered? Currently only handlers are scanned. Behaviors are registered manually. This is fine for now — ticket scope is limited to handler scanning convenience.

Same pattern should be applied to `Mediance.DependencyInjection.Autofac.AddMediance()`.

## Acceptance criteria

- [ ] `AddMediance(Type markerType)` overload exists for MS DI
- [ ] `AddMedianceFromEntryAssembly()` overload exists for MS DI
- [ ] Same overloads exist for Autofac
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
