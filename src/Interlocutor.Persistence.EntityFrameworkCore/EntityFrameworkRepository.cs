namespace Interlocutor.Persistence.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The EF Core <see cref="IDomainRepository{TEntity, TKey}"/> implementation, backed by a scoped
/// <typeparamref name="TContext"/>'s <see cref="DbSet{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository reads and writes.</typeparam>
/// <typeparam name="TKey">The entity's key type, used by <see cref="IDomainRepository{TEntity, TKey}.GetById"/>.</typeparam>
/// <typeparam name="TContext">The <see cref="DbContext"/> type providing database access.</typeparam>
/// <param name="context">
/// Lazy factory for the <typeparamref name="TContext"/> instance, matching <c>UnitOfWorkBehavior</c>'s own
/// <c>Lazy&lt;TContext&gt;</c> idiom so a dispatch that never touches this repository never pays for context
/// construction.
/// </param>
/// <param name="idSelector">
/// The expression projecting <typeparamref name="TEntity"/> to its <typeparamref name="TKey"/>, supplied by
/// the consumer at registration time - see <see cref="IdSelector"/>.
/// </param>
/// <remarks>
/// Registered scoped, resolved via <c>Lazy&lt;TContext&gt;</c>, matching <see cref="EntityFrameworkUnitOfWork{TContext}"/>'s
/// idiom so both share the same scoped <typeparamref name="TContext"/> instance within one dispatch.
/// </remarks>
public sealed class EntityFrameworkRepository<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEntity,
    TKey,
    TContext
>(Lazy<TContext> context, Expression<Func<TEntity, TKey>> idSelector) : IDomainRepository<TEntity, TKey>
    where TEntity : class
    where TContext : DbContext
{
    /// <inheritdoc/>
    public IQueryable<TEntity> Query => context.Value.Set<TEntity>();

    /// <inheritdoc/>
    public Expression<Func<TEntity, TKey>> IdSelector { get; } = idSelector;

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.Add(TEntity)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Value.Set<TEntity>().Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.Update(TEntity)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Value.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.Remove(TEntity)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Value.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="filter"/> and <paramref name="sort"/> are composed onto <see cref="Query"/> via
    /// <see cref="DomainRepositoryQueryableExtensions"/> before <see cref="EntityFrameworkQueryableExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
    /// executes the query, so filtering and ordering happen in the database, not in memory.
    /// </remarks>
    public async Task<IReadOnlyList<TEntity>> GetManyAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    ) => await this.Query.ApplyFilter(filter).ApplySort(sort).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="filter"/> is applied once and reused for both the <see cref="Page{T}.TotalCount"/>
    /// count query and the page's item query, so both agree on the same predicate. Sorting and paging are
    /// composed onto the query via <see cref="DomainRepositoryQueryableExtensions"/> before materialization,
    /// so the database performs the <c>ORDER BY</c>/<c>OFFSET</c>/<c>FETCH</c>, not this method.
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
        var totalCount = await filtered.CountAsync(cancellationToken);
        var items = await filtered.ApplySort(sort).ApplyPaging(pageNumber, pageSize).ToListAsync(cancellationToken);

        return new Page<TEntity>(items, pageNumber, pageSize, totalCount);
    }
}
