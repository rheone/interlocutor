namespace Interlocutor.Persistence.NHibernate;

using global::NHibernate;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// The NHibernate <see cref="IUnitOfWork"/> implementation - the transaction owner for the scoped
/// <see cref="ISession"/> for the current dispatch.
/// </summary>
/// <param name="session">
/// Lazy factory for the <see cref="ISession"/> instance, matching <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/>'s
/// own <c>Lazy&lt;ISession&gt;</c> idiom so a dispatch that never touches this unit of work never pays for
/// session construction.
/// </param>
/// <remarks>
/// Registered scoped, matching <see cref="ISession"/>'s own registration lifetime, so this unit of work
/// shares the same scoped session - and the same transaction - that
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> commits or rolls back at pipeline completion.
/// </remarks>
public sealed class NHibernateUnitOfWork(Lazy<ISession> session) : IUnitOfWork
{
    /// <inheritdoc/>
    /// <remarks>
    /// Begins a transaction first if one is not already open - covering the case where this method is the
    /// first write of the dispatch - then forwards to <see cref="ISession.FlushAsync(CancellationToken)"/>,
    /// not an EF Core-style <c>SaveChangesAsync</c>: NHibernate's <see cref="ISession"/> already tracks
    /// pending changes and flushes them on transaction commit, so this method exists for the case where a
    /// caller wants pending changes visible to subsequent queries within the same session before the
    /// transaction commits. The transaction stays open beyond this call's return; it is committed or rolled
    /// back by the commit-gate behavior at pipeline completion, not by this method.
    /// </remarks>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var s = session.Value;
        if (s.GetCurrentTransaction() is not { IsActive: true })
        {
            // Not captured/disposed here - UnitOfWorkBehavior retrieves this same session-owned transaction via
            // GetCurrentTransaction() and disposes it once, after commit or rollback, at pipeline completion.
#pragma warning disable IDISP004
            s.BeginTransaction();
#pragma warning restore IDISP004
        }

        await s.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task Cancel(CancellationToken cancellationToken = default) => this.CancelAsync(cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Rolls back and disposes the current transaction if one is active - mirroring
    /// <c>EntityFrameworkUnitOfWork{TContext}.CancelAsync</c>'s explicit, deterministic disposal rather than
    /// leaving it to the owning <see cref="ISession"/>'s own eventual disposal - then calls
    /// <see cref="ISession.Clear"/> to evict every entity from the session - discarding tracked state left
    /// inconsistent by the rollback - so <see cref="ISession.IsDirty()"/> reports <see langword="false"/>
    /// afterward and the commit-gate behavior's end-of-pipeline check finds nothing pending. A no-op if no
    /// transaction is active.
    /// </remarks>
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var s = session.Value;
        if (s.GetCurrentTransaction() is { IsActive: true } transaction)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            transaction.Dispose();
        }

        s.Clear();
    }
}
