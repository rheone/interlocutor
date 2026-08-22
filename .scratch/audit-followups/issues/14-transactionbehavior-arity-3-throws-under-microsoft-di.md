## Labels

ready-for-agent

## What to build

`InterlocutorPipelineBuilder.AddTransactionBehavior()` registers EF Core's `TransactionBehavior<TRequest,
TResponse, TContext>` (arity 3) against the open `IPipelineBehavior<TRequest, TResponse>` (arity 2) exactly
the way every other open generic behavior is registered - `AddBehavior(efCoreType)`, which
`ServiceCollectionExtensions.Apply` (Microsoft DI) translates to `services.AddScoped(typeof(IPipelineBehavior<,>),
typeof(TransactionBehavior<,,>))`. Reproduced directly, not just reported secondhand: this throws the moment
an application actually references `Interlocutor.EntityFrameworkCore` and builds a real `ServiceProvider`:

```
System.ArgumentException: Arity of open generic service type 'Interlocutor.Abstractions.Pipeline.IPipelineBehavior`2[TRequest,TResponse]' does not equal arity of open generic implementation type 'Interlocutor.EntityFrameworkCore.TransactionBehavior`3[TRequest,TResponse,TContext]'. (Parameter 'descriptors')
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.Populate()
   at Microsoft.Extensions.DependencyInjection.ServiceProvider..ctor(ICollection`1 serviceDescriptors, ServiceProviderOptions options)
```

Microsoft.Extensions.DependencyInjection requires an open generic service registration's implementation type
to have the same number of open type parameters as the service interface - it has no built-in way to leave a
third parameter (`TContext`) open behind a two-parameter interface. This is a different, narrower case than
ticket 13 (`AddBehavior` closed-generic-throws): ticket 13's `ResolveServiceType` fix only re-pairs a *closed*
implementation type with its own closed interface; it does nothing for an *open* implementation type whose
arity exceeds the service interface's arity, which is exactly EF Core's `TransactionBehavior<,,>` shape.

This has never been caught by CI because no existing Microsoft DI test project references
`Interlocutor.EntityFrameworkCore` and builds a real `ServiceProvider` with `AddTransactionBehavior()` -
`tests/Interlocutor.DependencyInjection.Microsoft.Tests` references only the NHibernate sibling (arity 2, no
`TContext` parameter, so no mismatch). Discovered building `samples/Interlocutor.Sample.WebApi` (ticket 18),
which needs the real EF Core transaction behavior under Microsoft DI and worked around it locally with a
per-app subclass (`SampleTransactionBehavior<TRequest,TResponse>` closing `TContext` over the app's own
`DbContext` type, registered via the general-purpose `AddBehavior` escape hatch) rather than
`AddTransactionBehavior()` itself - see that type's remarks in `samples/Interlocutor.Sample.WebApi/SampleTransactionBehavior.cs`.

Autofac does not have this problem - `ContainerBuilderExtensions.Apply`'s `RegisterGeneric(...).As(serviceType)`
accepts the arity mismatch directly, so `AddTransactionBehavior()` already works there today.

## Acceptance criteria

- [ ] `AddTransactionBehavior()` works against a real Microsoft DI `ServiceProvider` when
      `Interlocutor.EntityFrameworkCore` is referenced, without requiring the consuming application to write
      its own `TContext`-closing subclass
- [ ] A regression test in `tests/Interlocutor.DependencyInjection.Microsoft.Tests` references
      `Interlocutor.EntityFrameworkCore`, calls `AddTransactionBehavior()`, and builds a real `ServiceProvider`
      that resolves and dispatches a transactional request successfully
- [ ] No regression to the already-working NHibernate (arity 2) or Autofac (either store) paths
- [ ] If the fix is "the consuming application must supply `TContext` some other explicit way" rather than a
      structural change to the registration translator, that requirement is documented on `AddTransactionBehavior()`
      itself, not left for the next consumer to discover the hard way

## Blocked by

None - can start immediately
