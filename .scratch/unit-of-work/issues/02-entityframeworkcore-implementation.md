Status: needs-triage

## Labels

ready-for-agent

## What to build

New project `src/Interlocutor.Persistence.EntityFrameworkCore/Interlocutor.Persistence.EntityFrameworkCore.csproj`, referencing `Interlocutor.Persistence.Abstractions` (project reference) plus `Microsoft.EntityFrameworkCore`. Matching test project.

- `EntityFrameworkUnitOfWork<TContext> : IUnitOfWork` (or similar naming, matching `TransactionBehavior<TRequest,TResponse,TContext>`'s existing `TContext : DbContext` constraint pattern) - wraps a `Lazy<TContext>`, forwards `SaveChangesAsync` to `TContext.SaveChangesAsync`. Does not open its own transaction.
- `EntityFrameworkRepository<TEntity, TKey, TContext> : IDomainRepository<TEntity, TKey>` - CRUD via `DbSet<TEntity>`, get-many/get-paged via `IQueryable<TEntity>` (`DbSet<TEntity>.AsQueryable()`), filter/sort/paging applied via LINQ (`.Where`, `.OrderBy`/`.ThenBy` built from the sort spec, `.Skip`/`.Take` derived from page number/page size). Read [transactions-and-store-integration.md](../../../documentation/transactions-and-store-integration.md) for the existing `Lazy<TContext>` resolution idiom to match.
- Registered scoped, resolved via `Lazy<T>`, matching `TransactionBehavior`'s idiom - do not construct the context eagerly.
- `IsTrimmable=true`, not `IsAotCompatible`, matching `Interlocutor.EntityFrameworkCore`'s existing `.csproj` settings (ADR-0016).

## Acceptance criteria

- [ ] `Interlocutor.Persistence.EntityFrameworkCore` project exists, builds, references exactly `Interlocutor.Persistence.Abstractions` + `Microsoft.EntityFrameworkCore`
- [ ] `IUnitOfWork` and `IDomainRepository<TEntity,TKey>` both have EF Core implementations
- [ ] Filter/sort/paging translate to SQL (`Where`/`OrderBy`/`Skip`/`Take` composed on `IQueryable`, not materialized to `IEnumerable` before filtering - verify with a test asserting the generated query, not just the result, per the query-pushdown gotcha flagged in [research.md](../research.md) §2.1)
- [ ] Integration test (in-memory or SQLite provider) proves a repository call and a `SaveChangesAsync` through `IUnitOfWork` participate in the same `DbContext` instance as `TransactionBehavior` sees for the same dispatch
- [ ] `dotnet csharpier .` produces no diff
- [ ] `dotnet build` and `dotnet test` pass for the new test project

## Blocked by

01
