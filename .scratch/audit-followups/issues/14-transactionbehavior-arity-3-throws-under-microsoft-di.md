> **Status: done** - merged in `143a9b1`; see resolved note below.

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

- [x] `AddTransactionBehavior()` works against a real Microsoft DI `ServiceProvider` when
      `Interlocutor.EntityFrameworkCore` is referenced, without requiring the consuming application to write
      its own `TContext`-closing subclass — **Status: resolved via the documented-requirement fallback this
      criterion itself allows for (see the last bullet).** Microsoft.Extensions.DependencyInjection has no
      mechanism to leave a third open type parameter behind a two-parameter service interface at all - not a
      gap in this library's translator, a hard constraint of the container. Dynamically emitting the closing
      subclass at runtime (`Reflection.Emit`) was considered and rejected: it conflicts with this repo's
      shipped AOT/trimming commitment (`documentation/trim-and-aot-support.md`). `AddTransactionBehavior()`
      itself is unchanged and still a no-op-safe probe; the fix is that Microsoft DI now fails **immediately
      and actionably** at `AddInterlocutor()` call time instead of the previous cryptic, lazy
      `ArgumentException` at `BuildServiceProvider()` time - see `ServiceCollectionExtensions.ResolveServiceType`.
- [x] A regression test proves the documented workaround end-to-end: a small subclass closing `TContext` over
      a concrete `DbContext` (the same pattern `samples/Interlocutor.Sample.WebApi/SampleTransactionBehavior.cs`
      already uses), registered via `AddBehavior(typeof(...))`, resolves and dispatches a transactional
      request successfully through a real Microsoft DI `ServiceProvider`. A second regression test proves
      `AddTransactionBehavior()` itself now throws the new actionable `InvalidOperationException` (naming
      `AddBehavior` as the fix) instead of the old cryptic framework exception. Both live in a new, isolated
      `tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests` project - kept separate
      from `tests/Interlocutor.DependencyInjection.Microsoft.Tests` because referencing EF Core there flips
      `AddTransactionBehavior()`'s EF-Core-preferred probing and breaks that project's own NHibernate-arity-2
      assumptions (see `PipelineOrderingIntegrationTests` there).
- [x] No regression to the already-working NHibernate (arity 2) or Autofac (either store) paths - confirmed
      by the full existing `Interlocutor.DependencyInjection.Microsoft.Tests` and
      `Interlocutor.DependencyInjection.Autofac.Tests` suites still passing unchanged.
- [x] The `TContext`-must-be-supplied-explicitly requirement is documented directly on
      `AddTransactionBehavior()`'s XML doc remarks (with the concrete subclass pattern), not left for the
      next consumer to discover the hard way.

## Blocked by

None - can start immediately
