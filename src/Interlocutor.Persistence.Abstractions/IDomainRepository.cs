namespace Interlocutor.Persistence.Abstractions;

using System.Linq.Expressions;

/// <summary>
/// A store-agnostic CRUD/paging surface over a single entity type, backed by the same scoped session
/// <see cref="IUnitOfWork"/> wraps.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository reads and writes.</typeparam>
/// <typeparam name="TKey">The entity's key type, used by <see cref="GetById"/>.</typeparam>
/// <remarks>
/// <para>
/// Deliberately generic and unconstrained - there is no <c>IAggregateRoot</c> marker requirement.
/// Resolved via constructor injection per entity type (e.g. <c>IDomainRepository&lt;Order, Guid&gt;</c>
/// injected directly), not through an <see cref="IUnitOfWork"/> factory method, matching Interlocutor's
/// existing constructor-injection idiom.
/// </para>
/// <para>
/// <see cref="GetById"/> returns a nullable result rather than an <c>Outcome</c> case - per
/// <c>CLAUDE.md</c>'s result-type convention, only the handler decides which <c>Outcome&lt;...&gt;</c>
/// case a "not found" maps to. This package does not take a dependency on outcome-case semantics beyond
/// what <c>Interlocutor.Abstractions</c> already brings in.
/// </para>
/// </remarks>
public interface IDomainRepository<TEntity, TKey>
{
    /// <summary>
    /// Gets the escape hatch for ad hoc composition beyond this interface's fixed method set.
    /// </summary>
    /// <value>
    /// A queryable sequence of every <typeparamref name="TEntity"/> the underlying session can see. Both EF
    /// Core's <c>DbContext</c> and NHibernate's <c>ISession</c> expose LINQ providers, so this single member
    /// covers both implementations without an ORM-specific variant.
    /// </value>
    IQueryable<TEntity> Query { get; }

    /// <summary>
    /// Gets the expression that projects a <typeparamref name="TEntity"/> to its <typeparamref name="TKey"/>,
    /// used by <see cref="GetById"/> to build a key-equality predicate.
    /// </summary>
    /// <value>An expression such as <c>entity =&gt; entity.Id</c>, supplied by the implementation.</value>
    /// <remarks>
    /// <typeparamref name="TKey"/> alone does not say which property of <typeparamref name="TEntity"/> it
    /// comes from - this expression is what tells <see cref="GetById"/> which property to compare.
    /// </remarks>
    Expression<Func<TEntity, TKey>> IdSelector { get; }

    /// <summary>
    /// Adds a new entity to the underlying session, to be persisted the next time
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> runs.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that completes once the entity has been staged for insertion.</returns>
    Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity as modified in the underlying session, to be persisted the next time
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> runs.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that completes once the entity has been staged for update.</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity for removal from the underlying session, to be persisted the next time
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> runs.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that completes once the entity has been staged for deletion.</returns>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every <typeparamref name="TEntity"/> matching <paramref name="filter"/>, in the order
    /// described by <paramref name="sort"/>, without paging.
    /// </summary>
    /// <param name="filter">An optional predicate; <see langword="null"/> matches every entity.</param>
    /// <param name="sort">
    /// An optional ordered multi-column sort specification; <see langword="null"/> or empty leaves ordering
    /// unspecified.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the query to complete.</param>
    /// <returns>The matching entities, materialized into a list.</returns>
    Task<IReadOnlyList<TEntity>> GetManyAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves one page of <typeparamref name="TEntity"/> matching <paramref name="filter"/>, in the order
    /// described by <paramref name="sort"/>.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <param name="filter">An optional predicate; <see langword="null"/> matches every entity.</param>
    /// <param name="sort">
    /// An optional ordered multi-column sort specification; <see langword="null"/> or empty leaves ordering
    /// unspecified.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the query to complete.</param>
    /// <returns>The requested page, along with its paging metadata.</returns>
    Task<Page<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves the single entity whose key matches <paramref name="id"/>, or <see langword="null"/> if none does.
    /// </summary>
    /// <param name="id">The key value to look up.</param>
    /// <returns>The matching entity, or <see langword="null"/> if no entity has that key.</returns>
    /// <remarks>
    /// Ships as a default interface method built entirely on the <see cref="Query"/> escape hatch and
    /// <see cref="IdSelector"/> - implementations never need to override it. Synchronous by necessity: this
    /// package takes no ORM dependency, so it cannot call an ORM-specific async LINQ extension such as EF
    /// Core's <c>FirstOrDefaultAsync</c>; it can only rely on <see cref="System.Linq.Queryable"/>, which is
    /// synchronous.
    /// </remarks>
    TEntity? GetById(TKey id)
    {
        // A single capture of IdSelector: an expression-bodied property getter rebuilds its expression tree
        // (with a fresh ParameterExpression) on every access, so reading .Parameters and .Body from two
        // separate property accesses would pair a parameter from one tree with a body from another.
        var idSelector = this.IdSelector;
        var predicate = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(idSelector.Body, Expression.Constant(id, typeof(TKey))),
            idSelector.Parameters[0]
        );

        return this.Query.FirstOrDefault(predicate);
    }
}
