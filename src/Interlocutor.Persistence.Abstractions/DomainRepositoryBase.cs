namespace Interlocutor.Persistence.Abstractions;

using System.Linq.Expressions;

/// <summary>
/// Shared, store-agnostic base for <see cref="IDomainRepository{TEntity, TKey}"/> implementations. Supplies
/// <see cref="GetManyAsync"/> and <see cref="GetPagedAsync"/> once, built entirely on <see cref="Query"/> and
/// <see cref="DomainRepositoryQueryableExtensions"/>, so a derived, store-specific repository need not
/// duplicate that composition logic.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository reads and writes.</typeparam>
/// <typeparam name="TKey">The entity's key type, used by <see cref="IDomainRepository{TEntity, TKey}.GetById"/>.</typeparam>
/// <remarks>
/// <para>
/// This package takes no ORM dependency, so it cannot call an ORM-specific async LINQ materializer - EF
/// Core's or NHibernate's own <c>ToListAsync</c>/<c>CountAsync</c> extension methods - directly.
/// <see cref="ToListAsync"/> and <see cref="CountAsync"/> are the seam a derived type plugs its ORM's
/// materializer into; everything else - filter/sort/paging composition, reusing one filtered query for both
/// a page's item and total-count queries, and building the returned <see cref="Page{T}"/> - is shared here.
/// </para>
/// <para>
/// <see cref="Query"/>, <see cref="IdSelector"/>, and the genuinely store-specific <c>InsertAsync</c>/
/// <c>UpdateAsync</c>/<c>DeleteAsync</c> members remain abstract - a derived type still supplies those itself.
/// </para>
/// </remarks>
public abstract class DomainRepositoryBase<TEntity, TKey> : IDomainRepository<TEntity, TKey>
    where TEntity : class
{
    /// <inheritdoc/>
    public abstract IQueryable<TEntity> Query { get; }

    /// <inheritdoc/>
    public abstract Expression<Func<TEntity, TKey>> IdSelector { get; }

    /// <inheritdoc/>
    public abstract Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="filter"/> and <paramref name="sort"/> are composed onto <see cref="Query"/> via
    /// <see cref="DomainRepositoryQueryableExtensions"/> before <see cref="ToListAsync"/> executes the query,
    /// so filtering and ordering happen in the database, not in memory.
    /// </remarks>
    public async Task<IReadOnlyList<TEntity>> GetManyAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    ) => await this.ToListAsync(this.Query.ApplyFilter(filter).ApplySort(sort), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="filter"/> is applied once and reused for both the <see cref="Page{T}.TotalCount"/>
    /// count query and the page's item query, so both agree on the same predicate. Sorting and paging are
    /// composed onto the query via <see cref="DomainRepositoryQueryableExtensions"/> before materialization,
    /// so the database performs the ordering and row-limiting, not this method.
    /// </remarks>
    public async Task<Page<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    )
    {
        var filtered = this.Query.ApplyFilter(filter);
        var totalCount = await this.CountAsync(filtered, cancellationToken).ConfigureAwait(false);
        var items = await this.ToListAsync(filtered.ApplySort(sort).ApplyPaging(pageNumber, pageSize), cancellationToken)
            .ConfigureAwait(false);

        return new Page<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Materializes <paramref name="query"/> into a list, using the derived type's ORM-specific async LINQ
    /// provider (e.g. EF Core's or NHibernate's own <c>ToListAsync</c> extension method).
    /// </summary>
    /// <param name="query">The composed, still-unevaluated query to execute.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the query to complete.</param>
    /// <returns>The matching entities, materialized into a list.</returns>
    protected abstract Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the entities matched by <paramref name="query"/>, using the derived type's ORM-specific async
    /// LINQ provider (e.g. EF Core's or NHibernate's own <c>CountAsync</c> extension method).
    /// </summary>
    /// <param name="query">The composed, still-unevaluated query to count.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the query to complete.</param>
    /// <returns>The number of entities matching <paramref name="query"/>.</returns>
    protected abstract Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken);
}
