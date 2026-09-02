# Interlocutor.Persistence.Abstractions API Reference

Every public type in `Interlocutor.Persistence.Abstractions`, grouped by namespace - the store-neutral unit-of-work/repository abstraction behind the [transaction model](glossary.md#transaction-model) and [pagination](glossary.md#pagination).

## Interlocutor.Persistence.Abstractions

### `DomainRepositoryQueryableExtensions` class

Store-agnostic composition of an `IDomainRepository` filter/sort/paging specification onto an `IQueryable`, shared by every implementation so filter/sort/paging is composed once, on the query, and pushed down to the store - never materialized first.

| Member | Summary |
| --- | --- |
| `public static IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> query, Expression<Func<TEntity, bool>>? filter)` | Applies an optional filter predicate to `query`. |
| `public static IQueryable<TEntity> ApplyPaging<TEntity>(IQueryable<TEntity> query, int pageNumber, int pageSize)` | Applies a one-based page number and page size to `query` via Skip/Take. Callers apply `ApplyFilter` and `ApplySort` first. |
| `public static IQueryable<TEntity> ApplySort<TEntity>(IQueryable<TEntity> query, IReadOnlyList<SortCriterion<TEntity>>? sort)` | Applies an optional multi-column sort specification to `query`. |

### `IDomainRepository<TEntity, TKey>` interface

A store-agnostic CRUD/paging surface over a single entity type, backed by the same scoped session `IUnitOfWork` wraps.

| Member | Summary |
| --- | --- |
| `Expression<Func<TEntity, TKey>> IdSelector` | Gets the expression that projects a `TEntity` to its `TKey`, used by `GetById` to build a key-equality predicate. |
| `IQueryable<TEntity> Query` | Gets the escape hatch for ad hoc composition beyond this interface's fixed method set. |
| `Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` | Marks an existing entity for removal from the underlying session, to be persisted the next time `SaveChangesAsync` runs. |
| `TEntity? GetById(TKey id)` | Retrieves the single entity whose key matches `id`, or `null` if none does. |
| `Task<IReadOnlyList<TEntity>> GetManyAsync(Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` | Retrieves every `TEntity` matching `filter`, in the order described by `sort`, without paging. |
| `Task<Page<TEntity>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` | Retrieves one page of `TEntity` matching `filter`, in the order described by `sort`. |
| `Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` | Adds a new entity to the underlying session, to be persisted the next time `SaveChangesAsync` runs. |
| `Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` | Marks an existing entity as modified in the underlying session, to be persisted the next time `SaveChangesAsync` runs. |

### `IUnitOfWork` interface

A façade over the scoped store session (EF Core DbContext or NHibernate ISession) that also owns that session's transaction lifecycle for the current dispatch.

| Member | Summary |
| --- | --- |
| `Task Cancel(CancellationToken cancellationToken = default(CancellationToken))` | Immediately rolls back the current transaction, if one is open, discarding every change staged during this dispatch, and returns control to the caller. |
| `Task SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))` | Persists pending changes tracked by the underlying scoped session, opening a transaction first if one is not already open. |

### `Page<T>` record

One materialized page of results returned by `GetPagedAsync`, together with the paging metadata needed to render pagination controls.

| Member | Summary |
| --- | --- |
| `public Page(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)` |  |
| `public IReadOnlyList<T> Items` | The items belonging to `PageNumber`. |
| `public int PageNumber` | The one-based page number this page represents. |
| `public int PageSize` | The maximum number of items per page that was requested. |
| `public int TotalCount` | The total number of items across every page, ignoring paging. |
| `public int TotalPageCount` | Gets the total number of pages, given `TotalCount` and `PageSize`. |
| `public void Deconstruct(out IReadOnlyList<T> Items, out int PageNumber, out int PageSize, out int TotalCount)` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(Page<T>? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(Page<T>? left, Page<T>? right)` |  |
| `public static bool operator !=(Page<T>? left, Page<T>? right)` |  |

### `SortCriterion<TEntity>` record

One entry of a multi-column sort specification passed to `GetManyAsync` or `GetPagedAsync`.

| Member | Summary |
| --- | --- |
| `public SortCriterion(Expression<Func<TEntity, object>> KeySelector, SortDirection Direction)` |  |
| `public SortDirection Direction` | The direction to apply for this sort key. |
| `public Expression<Func<TEntity, object>> KeySelector` | The property to sort by, expressed as a lambda over `TEntity`. |
| `public void Deconstruct(out Expression<Func<TEntity, object>> KeySelector, out SortDirection Direction)` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SortCriterion<TEntity>? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(SortCriterion<TEntity>? left, SortCriterion<TEntity>? right)` |  |
| `public static bool operator !=(SortCriterion<TEntity>? left, SortCriterion<TEntity>? right)` |  |

### `SortDirection` enum

The direction to sort by for one entry of a `IDomainRepository` sort specification.

| Member | Summary |
| --- | --- |
| `Ascending = 0` | Sort from the lowest value to the highest. |
| `Descending = 1` | Sort from the highest value to the lowest. |

