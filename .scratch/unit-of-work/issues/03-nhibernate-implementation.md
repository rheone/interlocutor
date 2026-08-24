Status: closed (implemented)

## Labels

ready-for-agent

## What to build

New project `src/Interlocutor.Persistence.NHibernate/Interlocutor.Persistence.NHibernate.csproj`, referencing `Interlocutor.Persistence.Abstractions` (project reference) plus `NHibernate`. Matching test project.

- `NHibernateUnitOfWork : IUnitOfWork` - wraps a `Lazy<ISession>`, forwards to `ISession.FlushAsync` (NHibernate's `TransactionBehavior` already relies on flush-on-commit semantics - do not reimplement `SaveChangesAsync`-style explicit change tracking that EF Core needs but NHibernate doesn't; read `src/Interlocutor.NHibernate/TransactionBehavior.cs` first). Does not open its own transaction.
- `NHibernateRepository<TEntity, TKey> : IDomainRepository<TEntity, TKey>` - CRUD via `ISession` (`SaveAsync`/`UpdateAsync`/`DeleteAsync`/`GetAsync`), get-many/get-paged via `ISession.Query<TEntity>()` (NHibernate's LINQ provider) for the `IQueryable<TEntity>` escape hatch and the filter/sort/paging methods.
- Registered scoped, resolved via `Lazy<T>`, matching `TransactionBehavior`'s idiom.
- `IsTrimmable=true`, not `IsAotCompatible`, matching `Interlocutor.NHibernate`'s existing `.csproj` settings (ADR-0016).
- Respect NHibernate's own concurrency constraints (research.md §2.3): never share one `ISession`/`ITransaction` across concurrent threads or more than one per connection - this should fall out naturally from scoped-per-dispatch registration, but call it out in the implementation's XML docs as a documented constraint, not just an accident of DI lifetime.

## Acceptance criteria

- [x] `Interlocutor.Persistence.NHibernate` project exists, builds, references exactly `Interlocutor.Persistence.Abstractions` + `NHibernate`
- [x] `IUnitOfWork` and `IDomainRepository<TEntity,TKey>` both have NHibernate implementations
- [x] Filter/sort/paging translate through `ISession.Query<TEntity>()`'s LINQ provider, not materialized before filtering
- [x] Integration test proves a repository call and a flush through `IUnitOfWork` participate in the same `ISession` instance as `TransactionBehavior` sees for the same dispatch
- [x] `dotnet csharpier .` produces no diff
- [x] `dotnet build` and `dotnet test` pass for the new test project

## Blocked by

01

## Comments

Verified already implemented (commit 024a606): `NHibernateUnitOfWork` and `NHibernateRepository<TEntity,TKey>` exist, resolved via `Lazy<ISession>`. `dotnet test tests/Interlocutor.Persistence.NHibernate.Tests` passes (7/7).
