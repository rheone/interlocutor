Status: closed (implemented)

## Labels

ready-for-agent

## What to build

New project `src/Interlocutor.Persistence.Abstractions/Interlocutor.Persistence.Abstractions.csproj`, referencing only `Interlocutor.Abstractions` (project reference), zero ORM/container dependency. Matching test project `tests/Interlocutor.Persistence.Abstractions.Tests`.

Types (per [ADR-0017](../../../docs/adr/0017-optional-persistence-abstraction-repository-and-unit-of-work.md); one top-level public type per file per `AGENTS.md`):

- `IUnitOfWork` - façade only. At minimum a `SaveChangesAsync(CancellationToken)`-shaped member forwarding to the underlying scoped session/context. Does **not** expose `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` - that stays inside `TransactionBehavior`.
- `IDomainRepository<TEntity, TKey>`:
  - `GetById(TKey id)` - **C# default interface method**, implemented in terms of the `IQueryable<TEntity>` member below (e.g. `Queryable.FirstOrDefault(e => KeyEquals(e, id))` - work out the exact key-equality expression shape here, since `TKey` alone doesn't tell you which property to compare).
  - Insert, update, delete (async).
  - Get-many: takes an `Expression<Func<TEntity,bool>>` filter (nullable/optional) and the sort criteria below; returns an unpaged sequence.
  - Get-paged: takes the same filter and sort parameters plus page number and page size; returns the paging result type below.
  - `IQueryable<TEntity>` property/method as the escape hatch for ad hoc composition.
  - Get-single is nullable/`Maybe`-shaped - do not couple this package to `OperationOutcome` case semantics.
- Sort spec: an ordered collection of `(Expression<Func<TEntity,object>> KeySelector, SortDirection Direction)`. `SortDirection` is a small enum (`Ascending`/`Descending`).
- Paging result type (name TBD at implementation time, e.g. `PagedResult<T>` or `Page<T>` - pick one and use it consistently): items for the current page, current page number, page size, total item count, total page count. Lives in this package, not `Interlocutor.CommonKernel` - `CommonKernel`'s absence of a shipped paging type is unchanged (see ADR-0017's Alternatives section).
- Rich XML doc comments on every public member per `CLAUDE.md` - this is new public API surface, not exempt.

## Acceptance criteria

- [x] `Interlocutor.Persistence.Abstractions` project exists, builds, has zero ORM/container `PackageReference`
- [x] `IUnitOfWork`, `IDomainRepository<TEntity,TKey>`, sort spec type(s), `SortDirection`, and the paging result type all exist with full XML docs
- [x] `GetById` is a default interface method built on the `IQueryable<TEntity>` escape hatch; unit tests cover it against a fake in-memory `IQueryable<TEntity>` implementation
- [x] `dotnet csharpier .` produces no diff
- [x] `dotnet build` and `dotnet test tests/Interlocutor.Persistence.Abstractions.Tests` pass

## Blocked by

None - can start immediately.

## Comments

Verified already implemented (commit 024a606): `Interlocutor.Persistence.Abstractions` project builds with zero ORM/container dependency; `IUnitOfWork`, `IDomainRepository<TEntity,TKey>`, `SortCriterion`, `SortDirection`, and `Page<T>` all exist with XML docs. `dotnet test tests/Interlocutor.Persistence.Abstractions.Tests` passes (18/18). `dotnet csharpier check` is clean on this project.
