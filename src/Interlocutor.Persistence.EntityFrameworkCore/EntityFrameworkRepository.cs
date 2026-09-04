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
>(Lazy<TContext> context, Expression<Func<TEntity, TKey>> idSelector) : DomainRepositoryBase<TEntity, TKey>
    where TEntity : class
    where TContext : DbContext
{
    /// <inheritdoc/>
    public override IQueryable<TEntity> Query => context.Value.Set<TEntity>();

    /// <inheritdoc/>
    public override Expression<Func<TEntity, TKey>> IdSelector { get; } = idSelector;

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.AddAsync(TEntity, CancellationToken)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public override async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await context.Value.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.Update(TEntity)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public override Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Value.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>Stages the entity via <see cref="DbSet{TEntity}.Remove(TEntity)"/>; no query is issued until <see cref="IUnitOfWork.SaveChangesAsync"/> runs.</remarks>
    public override Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Value.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to EF Core's own <see cref="EntityFrameworkQueryableExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>.
    /// </remarks>
    protected override async Task<IReadOnlyList<TEntity>> ToListAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken
    ) => await query.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to EF Core's own <see cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>.
    /// </remarks>
    protected override Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken) =>
        query.CountAsync(cancellationToken);
}
