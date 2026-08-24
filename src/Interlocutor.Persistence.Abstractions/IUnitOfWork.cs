namespace Interlocutor.Persistence.Abstractions;

/// <summary>
/// A façade over the scoped store session (EF Core <c>DbContext</c> or NHibernate <c>ISession</c>) that
/// <c>TransactionBehavior</c> already manages for the current dispatch.
/// </summary>
/// <remarks>
/// <para>
/// This interface does not own transaction lifetime - there is no <c>BeginTransactionAsync</c>,
/// <c>CommitAsync</c>, or <c>RollbackAsync</c> here. Transaction begin/commit/rollback stays exactly
/// where it is today: inside <c>Interlocutor.EntityFrameworkCore.TransactionBehavior</c> or
/// <c>Interlocutor.NHibernate.TransactionBehavior</c>, driven by <c>ITransactionalRequest</c> and the
/// outcome classifier. An <see cref="IUnitOfWork"/> implementation wraps the same scoped session those
/// behaviors already resolve for the current dispatch - it does not introduce a second, competing
/// transaction-ownership model.
/// </para>
/// <para>
/// Registered scoped and resolved via <c>Lazy&lt;IUnitOfWork&gt;</c> by consumers who want deferred
/// construction, the same idiom <c>TransactionBehavior</c> itself uses so a dispatch that never touches
/// storage never pays for session construction. This type does not implement <see cref="IAsyncDisposable"/>
/// itself - disposal of the underlying session remains the DI container's job.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists pending changes tracked by the underlying scoped session.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the persistence to complete.</param>
    /// <returns>A task that completes once pending changes have been forwarded to the underlying session.</returns>
    /// <remarks>
    /// For EF Core, this forwards to <c>DbContext.SaveChangesAsync</c>. For NHibernate, whose <c>ISession</c>
    /// already tracks pending changes and flushes them on transaction commit, this forwards to
    /// <c>ISession.FlushAsync</c>. Calling this outside of a request wrapped by <c>TransactionBehavior</c>
    /// persists changes without transactional atomicity - the caller is responsible for that decision.
    /// </remarks>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
