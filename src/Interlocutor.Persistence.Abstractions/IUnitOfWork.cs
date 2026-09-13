namespace Interlocutor.Persistence.Abstractions;

/// <summary>
/// A façade over the scoped store session (EF Core <c>DbContext</c> or NHibernate <c>ISession</c>) that also
/// owns that session's transaction lifecycle for the current dispatch.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IUnitOfWork"/> is the transaction owner - not a pure façade over a transaction some other
/// component manages. Owning the transaction directly gives storage work one vocabulary instead of two, and
/// lets the transaction boundary bind to the first actual write instead of to a declared marker plus pipeline
/// position. The store-specific commit-gate behavior (<c>Interlocutor.Persistence.EntityFrameworkCore</c>'s or
/// <c>Interlocutor.Persistence.NHibernate</c>'s <c>UnitOfWorkBehavior</c>) opens a transaction lazily the first
/// time anything - an <see cref="IDomainRepository{TEntity, TKey}"/>, this interface, or a directly-injected
/// <c>Lazy&lt;TContext&gt;</c>/<c>Lazy&lt;ISession&gt;</c> - performs a write, and commits it automatically at
/// pipeline completion. No response case type decides that: commit is the default whenever anything is left
/// pending, overridden only by an explicit no-commit signal (<see cref="CancelAsync"/>, or a call-site opt-out
/// the handler itself makes) or by an unhandled exception. A dispatch that only ever reads never pays for an
/// open transaction.
/// </para>
/// <para>
/// Registered scoped and resolved via <c>Lazy&lt;IUnitOfWork&gt;</c> by consumers who want deferred
/// construction, the same idiom the commit-gate behavior itself uses so a dispatch that never touches storage
/// never pays for session construction. This type does not implement <see cref="IAsyncDisposable"/> itself -
/// disposal of the underlying session remains the DI container's job.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists pending changes tracked by the underlying scoped session, opening a transaction first if one
    /// is not already open.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the persistence to complete.</param>
    /// <returns>A task that completes once pending changes have been forwarded to the underlying session.</returns>
    /// <remarks>
    /// For EF Core, this forwards to <c>DbContext.SaveChangesAsync</c>. For NHibernate, whose <c>ISession</c>
    /// already tracks pending changes and flushes them on transaction commit, this forwards to
    /// <c>ISession.FlushAsync</c>. Either way, the transaction this call opens (if one is not already open)
    /// stays open beyond this call's return - it is committed or rolled back by the commit-gate behavior at
    /// pipeline completion, not by this method.
    /// </remarks>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately rolls back the current transaction, if one is open, discarding every change staged during
    /// this dispatch, and returns control to the caller.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the rollback to complete.</param>
    /// <returns>A task that completes once the rollback (if any was needed) has finished.</returns>
    /// <remarks>
    /// <para>
    /// This is a rare-case escape hatch for a handler that wants to force a rollback without being obligated
    /// to also return a classified failure-shaped <c>Outcome</c>. <see cref="CancelAsync"/> is not a
    /// short-circuit primitive and does not throw or otherwise interrupt control flow - it rolls back and
    /// returns, and the handler decides what outcome to return afterward. The pipeline's automatic commit
    /// gate simply finds nothing pending to commit at completion, the same as if the dispatch had never
    /// written anything.
    /// </para>
    /// <para>
    /// A no-op if no transaction is currently open (for example, a pure-read dispatch, or a dispatch where
    /// <see cref="CancelAsync"/> has already run once).
    /// </para>
    /// </remarks>
    Task CancelAsync(CancellationToken cancellationToken = default);
}
