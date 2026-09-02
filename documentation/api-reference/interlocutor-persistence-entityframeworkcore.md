# Interlocutor.Persistence.EntityFrameworkCore API Reference

Every public type in `Interlocutor.Persistence.EntityFrameworkCore`, grouped by namespace - the EF Core implementation of the persistence abstraction, including its [transaction model](../glossary.md#transaction-model)'s commit-gate behavior.

## Interlocutor.Persistence.EntityFrameworkCore

### `EntityFrameworkRepository<TEntity, TKey, TContext>` sealed class

The EF Core `IDomainRepository` implementation, backed by a scoped `TContext`'s `DbSet`.

| Member | Summary |
| --- | --- |
| `public EntityFrameworkRepository(Lazy<TContext> context, Expression<Func<TEntity, TKey>> idSelector)` |  |
| `public Expression<Func<TEntity, TKey>> IdSelector` |  |
| `public IQueryable<TEntity> Query` |  |
| `public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task<IReadOnlyList<TEntity>> GetManyAsync(Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task<Page<TEntity>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |

### `EntityFrameworkUnitOfWork<TContext>` sealed class

The EF Core `IUnitOfWork` implementation - the transaction owner for the scoped `TContext` for the current dispatch.

| Member | Summary |
| --- | --- |
| `public EntityFrameworkUnitOfWork(Lazy<TContext> context)` |  |
| `public Task Cancel(CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))` |  |

### `UnitOfWorkBehavior<TRequest, TResponse, TContext>` class

The EF Core commit-gate pipeline behavior: commits or rolls back whatever transaction a dispatch opened, per the unit-of-work-owned transaction model.

| Member | Summary |
| --- | --- |
| `public UnitOfWorkBehavior(Lazy<TContext> db, IOutcomeClassifier? classifier = null, IResponseOutcomeDescriber? describer = null, ILogger<UnitOfWorkBehavior<TRequest, TResponse, TContext>>? logger = null)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` |  |

### `UnitOfWorkSaveChangesInterceptor` sealed class

An EF Core `SaveChangesInterceptor` that opens a transaction on the `DbContext` before any SaveChanges/SaveChangesAsync call reaches the database, if one is not already open.

| Member | Summary |
| --- | --- |
| `public UnitOfWorkSaveChangesInterceptor()` |  |
| `public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)` |  |
| `public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default(CancellationToken))` |  |

