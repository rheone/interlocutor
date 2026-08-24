namespace Interlocutor.Persistence.NHibernate;

using global::NHibernate;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// The NHibernate <see cref="IUnitOfWork"/> implementation - a façade over the scoped <see cref="ISession"/>
/// that <c>Interlocutor.NHibernate.TransactionBehavior{TRequest,TResponse}</c> already manages.
/// </summary>
/// <param name="session">
/// Lazy factory for the <see cref="ISession"/> instance, matching <c>TransactionBehavior</c>'s own
/// <c>Lazy&lt;ISession&gt;</c> idiom so a dispatch that never touches this unit of work never pays for
/// session construction.
/// </param>
/// <remarks>
/// Does not begin, commit, or roll back a transaction - see <see cref="IUnitOfWork"/>'s remarks. Registered
/// scoped, matching <see cref="ISession"/>'s own registration lifetime.
/// </remarks>
public sealed class NHibernateUnitOfWork(Lazy<ISession> session) : IUnitOfWork
{
    /// <inheritdoc/>
    /// <remarks>
    /// Forwards to <see cref="ISession.FlushAsync(CancellationToken)"/>, not an EF Core-style
    /// <c>SaveChangesAsync</c>: NHibernate's <see cref="ISession"/> already tracks pending changes and
    /// flushes them on transaction commit, so this method exists for the case where a caller wants pending
    /// changes visible to subsequent queries within the same session before the transaction commits.
    /// </remarks>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => session.Value.FlushAsync(cancellationToken);
}
