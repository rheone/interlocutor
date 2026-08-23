namespace Interlocutor.Persistence.EntityFrameworkCore;

using Interlocutor.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The EF Core <see cref="IUnitOfWork"/> implementation - a façade over the scoped <typeparamref name="TContext"/>
/// that <c>Interlocutor.EntityFrameworkCore.TransactionBehavior{TRequest,TResponse,TContext}</c> already manages.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type providing database access.</typeparam>
/// <param name="context">
/// Lazy factory for the <typeparamref name="TContext"/> instance, matching <c>TransactionBehavior</c>'s own
/// <c>Lazy&lt;TContext&gt;</c> idiom so a dispatch that never touches this unit of work never pays for
/// context construction.
/// </param>
/// <remarks>
/// Does not begin, commit, or roll back a transaction - see <see cref="IUnitOfWork"/>'s remarks. Registered
/// scoped, matching <typeparamref name="TContext"/>'s own registration lifetime.
/// </remarks>
public sealed class EntityFrameworkUnitOfWork<TContext>(Lazy<TContext> context) : IUnitOfWork
    where TContext : DbContext
{
    /// <inheritdoc/>
    /// <remarks>Forwards to <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.</remarks>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.Value.SaveChangesAsync(cancellationToken);
}
