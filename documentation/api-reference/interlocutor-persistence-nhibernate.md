# Interlocutor.Persistence.NHibernate API Reference

Every public type in `Interlocutor.Persistence.NHibernate`, grouped by namespace - the NHibernate implementation of the persistence abstraction, including its [transaction model](../glossary.md#transaction-model)'s commit-gate behavior.

## Interlocutor.Persistence.NHibernate

### `NHibernateRepository<TEntity, TKey>` sealed class

The NHibernate `IDomainRepository` implementation, backed by a scoped `ISession`.

| Member | Summary |
| --- | --- |
| `public NHibernateRepository(Lazy<ISession> session, Expression<Func<TEntity, TKey>> idSelector)` |  |
| `public Expression<Func<TEntity, TKey>> IdSelector` |  |
| `public IQueryable<TEntity> Query` |  |
| `public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task<IReadOnlyList<TEntity>> GetManyAsync(Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task<Page<TEntity>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? filter = null, IReadOnlyList<SortCriterion<TEntity>>? sort = null, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))` |  |

### `NHibernateUnitOfWork` sealed class

The NHibernate `IUnitOfWork` implementation - the transaction owner for the scoped `ISession` for the current dispatch.

| Member | Summary |
| --- | --- |
| `public NHibernateUnitOfWork(Lazy<ISession> session)` |  |
| `public Task Cancel(CancellationToken cancellationToken = default(CancellationToken))` |  |
| `public Task SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))` |  |

### `UnitOfWorkBehavior<TRequest, TResponse>` sealed class

The NHibernate commit-gate pipeline behavior: commits or rolls back whatever transaction a dispatch opened, per the unit-of-work-owned transaction model.

| Member | Summary |
| --- | --- |
| `public UnitOfWorkBehavior(Lazy<ISession> session, IOutcomeClassifier? classifier = null, IResponseOutcomeDescriber? describer = null, ILogger<UnitOfWorkBehavior<TRequest, TResponse>>? logger = null)` |  |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` |  |

