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
    : DomainRepositoryBase<TEntity, TKey>
    where TEntity : class
{
    /// <inheritdoc/>
    /// <remarks>Backed by NHibernate's LINQ provider via <see cref="ISession.Query{T}()"/>.</remarks>
    public override IQueryable<TEntity> Query => session.Value.Query<TEntity>();

    /// <inheritdoc/>
    public override Expression<Func<TEntity, TKey>> IdSelector { get; } = idSelector;

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the entity via <see cref="ISession.SaveAsync(object, CancellationToken)"/>; NHibernate flushes
    /// it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public override async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await session.Value.SaveAsync(entity, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the entity via <see cref="ISession.UpdateAsync(object, CancellationToken)"/>; NHibernate
    /// flushes it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public override Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        session.Value.UpdateAsync(entity, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Stages the removal via <see cref="ISession.DeleteAsync(object, CancellationToken)"/>; NHibernate
    /// flushes it to the database on transaction commit (or an explicit <see cref="IUnitOfWork.SaveChangesAsync"/>),
    /// not immediately.
    /// </remarks>
    public override Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        session.Value.DeleteAsync(entity, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to NHibernate's own <see cref="LinqExtensionMethods.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>.
    /// </remarks>
    protected override async Task<IReadOnlyList<TEntity>> ToListAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken
    ) => await query.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to NHibernate's own <see cref="LinqExtensionMethods.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>.
    /// </remarks>
    protected override Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken) =>
        query.CountAsync(cancellationToken);
}
