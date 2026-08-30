namespace Interlocutor.Persistence.NHibernate;

using System.Linq.Expressions;
using global::NHibernate;
using global::NHibernate.Linq;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// The NHibernate <see cref="IDomainRepository{TEntity, TKey}"/> implementation, backed by a scoped
/// <see cref="ISession"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository reads and writes.</typeparam>
/// <typeparam name="TKey">The entity's key type, used by <see cref="IDomainRepository{TEntity, TKey}.GetById"/>.</typeparam>
/// <param name="session">
/// Lazy factory for the <see cref="ISession"/> instance, matching <c>UnitOfWorkBehavior</c>'s own
/// <c>Lazy&lt;ISession&gt;</c> idiom so a dispatch that never touches this repository never pays for session
/// construction.
/// </param>
/// <param name="idSelector">
/// The expression projecting <typeparamref name="TEntity"/> to its <typeparamref name="TKey"/>, supplied by
/// the consumer at registration time - see <see cref="IdSelector"/>.
/// </param>
/// <remarks>
/// <para>
/// Registered scoped, resolved via <c>Lazy&lt;ISession&gt;</c>, matching <see cref="NHibernateUnitOfWork"/>'s
/// idiom so both share the same scoped <see cref="ISession"/> instance within one dispatch.
/// </para>
/// <para>
/// Never share the <see cref="ISession"/> this repository wraps across concurrent threads, or use it with
/// more than one open <see cref="ITransaction"/> per connection - NHibernate's own concurrency contract for
/// <see cref="ISession"/>. Registering this repository scoped, one session per dispatch, satisfies that
/// contract by construction; it is documented here because it is a real constraint on how a consumer may
/// use an injected instance, not an implementation detail this type enforces at runtime.
/// </para>
/// </remarks>
public sealed class NHibernateRepository<TEntity, TKey>(Lazy<ISession> session, Expression<Func<TEntity, TKey>> idSelector)
    : IDomainRepository<TEntity, TKey>
    where TEntity : class
{
    /// <inheritdoc/>
    /// <remarks>Backed by NHibernate's LINQ provider via <c>ISession.Query&lt;TEntity&gt;()</c>.</remarks>
    public IQueryable<TEntity> Query => session.Value.Query<TEntity>();

    /// <inheritdoc/>
    public Expression<Func<TEntity, TKey>> IdSelector { get; } = idSelector;

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the entity via <see cref="ISession.SaveAsync(object, CancellationToken)"/>; NHibernate flushes
    /// it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await session.Value.SaveAsync(entity, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the entity via <see cref="ISession.UpdateAsync(object, CancellationToken)"/>; NHibernate
    /// flushes it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        session.Value.UpdateAsync(entity, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the removal via <see cref="ISession.DeleteAsync(object, CancellationToken)"/>; NHibernate
    /// flushes it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        session.Value.DeleteAsync(entity, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="filter"/> and <paramref name="sort"/> are composed onto <see cref="Query"/> via
    /// <see cref="DomainRepositoryQueryableExtensions"/> before NHibernate's LINQ provider translates and
    /// executes the query, so filtering and ordering happen in the database, not in memory.
    /// </remarks>
    public async Task<IReadOnlyList<TEntity>> GetManyAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        IReadOnlyList<SortCriterion<TEntity>>? sort = null,
        CancellationToken cancellationToken = default
    ) => await this.Query.ApplyFilter(filter).ApplySort(sort).ToListAsync(cancellationToken).ConfigureAwait(false);

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
        var totalCount = await filtered.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await filtered
            .ApplySort(sort)
            .ApplyPaging(pageNumber, pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new Page<TEntity>(items, pageNumber, pageSize, totalCount);
    }
}
