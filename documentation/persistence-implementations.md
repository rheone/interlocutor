# Persistence Implementations

The concrete `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` implementations - `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate` - and the query-composition helpers both build on. [Persistence Abstraction](persistence-abstraction.md) covers the store-agnostic interfaces and how to register one; this doc covers what each store's implementation actually does underneath them, so a consumer choosing between EF Core and NHibernate (or debugging a repository call) can see the real behavior rather than the interface contract alone.

## Shared Query Composition

Both implementations build `GetManyAsync`/`GetPagedAsync` from the same store-agnostic extension methods, so filter, sort, and paging logic exists exactly once:

```csharp
public static class DomainRepositoryQueryableExtensions
{
    public static IQueryable<TEntity> ApplyFilter<TEntity>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, bool>>? filter);

    public static IQueryable<TEntity> ApplySort<TEntity>(
        this IQueryable<TEntity> query,
        IReadOnlyList<SortCriterion<TEntity>>? sort);

    public static IQueryable<TEntity> ApplyPaging<TEntity>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize);
}
```

| Method | Behavior |
| :-- | :-- |
| `ApplyFilter` | `query.Where(filter)` when `filter` is non-null; `query` unchanged otherwise. |
| `ApplySort` | Composes `sort` into an `OrderBy`/`OrderByDescending` followed by one `ThenBy`/`ThenByDescending` per remaining entry, in list order - `null` or empty leaves ordering unspecified. |
| `ApplyPaging` | `query.Skip((pageNumber - 1) * pageSize).Take(pageSize)`. Throws `ArgumentOutOfRangeException` if `pageNumber` or `pageSize` is less than `1`. |

> [!IMPORTANT]
> None of these methods enumerate `query` - each returns a new, still-unevaluated `IQueryable<TEntity>`. Both EF Core's and NHibernate's LINQ providers translate the composed query to SQL only when the caller materializes it (`ToListAsync`/`CountAsync`), so filtering, ordering, and row-limiting all happen in the database, never in memory.

Both `GetPagedAsync` implementations apply `ApplyFilter` once and reuse the filtered query for both the `Page<T>.TotalCount` count query and the page's item query, so the two agree on the same predicate - `ApplySort`/`ApplyPaging` are then composed only onto the item query, since a count query has no use for ordering or row-limiting.

## Interlocutor.Persistence.EntityFrameworkCore

### `EntityFrameworkUnitOfWork<TContext>`

```csharp
public sealed class EntityFrameworkUnitOfWork<TContext>(Lazy<TContext> context) : IUnitOfWork
    where TContext : DbContext
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`SaveChangesAsync` begins a transaction first if one isn't already open, then forwards to `DbContext.SaveChangesAsync(CancellationToken)`; `Cancel` rolls back the current transaction (if any) and clears the change tracker. The constructor takes `Lazy<TContext>`, not `TContext` - the same idiom `UnitOfWorkBehavior` uses, so a dispatch that never touches this unit of work never pays for context construction. Registered scoped, matching `TContext`'s own registration lifetime.

### `EntityFrameworkRepository<TEntity, TKey, TContext>`

```csharp
public sealed class EntityFrameworkRepository<TEntity, TKey, TContext>(
    Lazy<TContext> context,
    Expression<Func<TEntity, TKey>> idSelector) : IDomainRepository<TEntity, TKey>
    where TEntity : class
    where TContext : DbContext
{
    public IQueryable<TEntity> Query => context.Value.Set<TEntity>();
    public Expression<Func<TEntity, TKey>> IdSelector { get; }

    public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TEntity>> GetManyAsync(...);
    public Task<Page<TEntity>> GetPagedAsync(...);
}
```

| Member | Behavior |
| :-- | :-- |
| `Query` | `context.Value.Set<TEntity>()` - the raw `DbSet<TEntity>`. |
| `InsertAsync` | `DbSet<TEntity>.Add(entity)` - stages the entity in the change tracker; no query is issued until `SaveChangesAsync` runs. |
| `UpdateAsync` | `DbSet<TEntity>.Update(entity)` - same staging semantics. |
| `DeleteAsync` | `DbSet<TEntity>.Remove(entity)` - same staging semantics. |
| `GetManyAsync` | `Query.ApplyFilter(filter).ApplySort(sort).ToListAsync(cancellationToken)`. |
| `GetPagedAsync` | Filters once, counts, then applies sort + paging to the same filtered query before materializing - see [Shared Query Composition](#shared-query-composition) above. |

`TEntity` carries a `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]` annotation on its type parameter, so EF Core's own model-building reflection over `TEntity` stays visible to a trimmer - see [Trim & AOT Support](trim-and-aot-support.md).

```csharp
services.AddInterlocutorRepository<Order, int, EntityFrameworkRepository<Order, int, AppDbContext>>(sp =>
    new EntityFrameworkRepository<Order, int, AppDbContext>(
        new Lazy<AppDbContext>(sp.GetRequiredService<AppDbContext>),
        order => order.Id));
```

## Interlocutor.Persistence.NHibernate

### `NHibernateUnitOfWork`

```csharp
public sealed class NHibernateUnitOfWork(Lazy<ISession> session) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`SaveChangesAsync` begins a transaction first if one isn't already active, then forwards to `ISession.FlushAsync(CancellationToken)`, not an EF Core-style change-tracker commit: NHibernate's `ISession` already tracks pending changes and flushes them automatically on transaction commit, so calling this explicitly is for the case where a handler wants pending changes visible to a *subsequent query within the same session*, before the transaction commits. `Cancel` rolls back the current transaction (if active) and calls `ISession.Clear()`. The constructor takes `Lazy<ISession>`, matching `UnitOfWorkBehavior`'s own idiom.

### `NHibernateRepository<TEntity, TKey>`

```csharp
public sealed class NHibernateRepository<TEntity, TKey>(
    Lazy<ISession> session,
    Expression<Func<TEntity, TKey>> idSelector) : IDomainRepository<TEntity, TKey>
    where TEntity : class
{
    public IQueryable<TEntity> Query => session.Value.Query<TEntity>();
    public Expression<Func<TEntity, TKey>> IdSelector { get; }

    public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TEntity>> GetManyAsync(...);
    public Task<Page<TEntity>> GetPagedAsync(...);
}
```

| Member | Behavior |
| :-- | :-- |
| `Query` | `session.Value.Query<TEntity>()` - NHibernate's LINQ provider. |
| `InsertAsync` | `ISession.SaveAsync(entity, cancellationToken)` - staged; NHibernate flushes it to the database on transaction commit (or an explicit `IUnitOfWork.SaveChangesAsync`), not immediately. |
| `UpdateAsync` | `ISession.UpdateAsync(entity, cancellationToken)` - same staging semantics. |
| `DeleteAsync` | `ISession.DeleteAsync(entity, cancellationToken)` - same staging semantics. |
| `GetManyAsync` | `Query.ApplyFilter(filter).ApplySort(sort).ToListAsync(cancellationToken)`. |
| `GetPagedAsync` | Filters once, counts, then applies sort + paging to the same filtered query before materializing - see [Shared Query Composition](#shared-query-composition) above. |

```csharp
containerBuilder.AddInterlocutorRepository<Order, int, NHibernateRepository<Order, int>>(c =>
    new NHibernateRepository<Order, int>(c.Resolve<Lazy<ISession>>(), order => order.Id));
```

> [!WARNING]
> Never share the `ISession` an `NHibernateRepository<TEntity, TKey>` wraps across concurrent threads, or use it with more than one open transaction per connection - NHibernate's own concurrency contract for `ISession`. Registering this repository scoped, one session per dispatch, satisfies that contract by construction; see [Persistence Abstraction § NHibernate session concurrency](persistence-abstraction.md#nhibernate-session-concurrency).

## EF Core vs. NHibernate: Behavioral Differences

Both implementations satisfy the identical `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` contract, but the underlying store shapes a few observable differences a consumer moving between the two should expect:

| | EF Core | NHibernate |
| :-- | :-- | :-- |
| `IUnitOfWork.SaveChangesAsync` forwards to | `DbContext.SaveChangesAsync` | `ISession.FlushAsync` |
| `InsertAsync`/`UpdateAsync`/`DeleteAsync` stage via | `DbSet<TEntity>.Add`/`Update`/`Remove` (synchronous change-tracker calls, wrapped in a completed `Task`) | `ISession.SaveAsync`/`UpdateAsync`/`DeleteAsync` (genuinely asynchronous NHibernate calls) |
| `Query` backed by | `DbSet<TEntity>` | `ISession.Query<TEntity>()` |
| Session/context concurrency contract | One `DbContext` instance is not thread-safe for concurrent operations | One `ISession` instance must not be shared across threads or more than one open transaction - see the `[!WARNING]` above |

Neither implementation issues a database round-trip from `InsertAsync`/`UpdateAsync`/`DeleteAsync` themselves - both only stage the change in the session/context's own tracking, deferring the actual write to `SaveChangesAsync` (explicit or via `UnitOfWorkBehavior`'s commit). This symmetry is deliberate: a handler written against `IDomainRepository<TEntity, TKey>` behaves identically regardless of which store package is referenced.

## See Also

- [Persistence Abstraction](persistence-abstraction.md) - the store-agnostic `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` interfaces, `Page<T>`, `SortCriterion<TEntity>`, and how to register an implementation per container.
- [Transactions & Store Integration](transactions-and-store-integration.md) - `UnitOfWorkBehavior`, the same scoped session both a repository and the unit-of-work behavior resolve, begin-on-first-write, and commit/rollback semantics.
- [Dependency Injection Packages](dependency-injection-packages.md) - the `AddInterlocutorUnitOfWork`/`AddInterlocutorRepository` registration extensions these implementations are registered through.
- [Trim & AOT Support](trim-and-aot-support.md) - the `DynamicallyAccessedMembers` annotation on `EntityFrameworkRepository<TEntity, TKey, TContext>`'s `TEntity` parameter.
