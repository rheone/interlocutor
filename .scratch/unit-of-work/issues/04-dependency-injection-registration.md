Status: needs-triage

## Labels

ready-for-agent

## What to build

Wire scoped registration for `IUnitOfWork` and `IDomainRepository<TEntity,TKey>` into both `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.DependencyInjection.Autofac`, following whatever extension-method pattern those packages already use for opt-in registration (read `ServiceCollectionExtensions.cs` / `ContainerBuilderExtensions.cs` first - this is new opt-in surface, not part of the default `AddInterlocutor()` chain per ADR-0009's "no default pipeline" philosophy carried over to this optional layer).

- Both `IUnitOfWork` and `IDomainRepository<TEntity,TKey>` implementations register **scoped**, matching `DbContext`/`ISession`'s own registration lifetime (see `documentation/transactions-and-store-integration.md`'s existing registration examples).
- Registration must be explicit opt-in (a dedicated builder call or extension method the consumer calls themselves) - do not fold this into `AddInterlocutor()`'s default surface, consistent with this whole feature being optional per ADR-0017.
- Decide and document whether `IDomainRepository<TEntity,TKey>` registration is per-entity-type (consumer registers each entity's repository explicitly) or a generic open registration the container closes per requested `TEntity`/`TKey` pair (mirroring how `TransactionBehavior<TRequest,TResponse,TContext>` is registered as an open generic) - confirm which container APIs make each option available before committing, since Autofac and Microsoft.Extensions.DependencyInjection differ here.

## Acceptance criteria

- [ ] A consumer can opt into the persistence abstraction with one explicit call per container (not part of `AddInterlocutor()`'s implicit defaults)
- [ ] `IUnitOfWork` and `IDomainRepository<TEntity,TKey>` resolve scoped in both containers
- [ ] A test in each DI test project (`Interlocutor.DependencyInjection.Microsoft.Tests`, `Interlocutor.DependencyInjection.Autofac.Tests`) proves the resolved `IUnitOfWork` and a resolved `IDomainRepository<TEntity,TKey>` share the same underlying `DbContext`/`ISession` instance within one scope
- [ ] `dotnet build` and `dotnet test` pass

## Blocked by

02, 03
