namespace Interlocutor.Persistence.Abstractions;

/// <summary>
/// The store-facing seam <see cref="CommitGateDriver"/> drives to run the shared begin/commit/rollback
/// algorithm against a store's scoped session, without the driver itself depending on any particular ORM.
/// </summary>
/// <remarks>
/// Each store package ships its own small internal adapter implementing this interface - EF Core's
/// <c>EntityFrameworkCommitGateSession</c> wrapping a <c>DbContext</c>, NHibernate's
/// <c>NHibernateCommitGateSession</c> wrapping an <c>ISession</c> - translating the driver's calls into that
/// store's own transaction API. That store's <c>UnitOfWorkBehavior</c> constructs the adapter and hands it to
/// <see cref="CommitGateDriver.HandleAsync{TRequest, TResponse}"/> as a <c>sessionFactory</c>; the driver never
/// inspects a concrete transaction object itself, and every decision is made through the members below.
/// </remarks>
public interface ICommitGateSession
{
    /// <summary>Gets a value indicating whether a transaction is currently open on this session.</summary>
    /// <value><see langword="true"/> if a transaction is open and active; otherwise, <see langword="false"/>.</value>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Determines whether the session has changes staged that have not yet been committed, independent of
    /// whether a transaction has been explicitly begun - for example, a write staged directly against the
    /// underlying session with no transaction open yet.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while checking for pending work.</param>
    /// <returns>
    /// A task whose result is <see langword="true"/> if the session has unpersisted changes staged, even
    /// when <see cref="HasActiveTransaction"/> is <see langword="false"/>.
    /// </returns>
    Task<bool> HasPendingWorkAsync(CancellationToken cancellationToken);

    /// <summary>Begins a new transaction on this session.</summary>
    /// <param name="cancellationToken">A token to observe while beginning the transaction.</param>
    /// <returns>A task that completes once the transaction has begun.</returns>
    /// <remarks>Only ever called by <see cref="CommitGateDriver"/> while <see cref="HasActiveTransaction"/> is <see langword="false"/>.</remarks>
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Commits the session's currently active transaction and disposes it.</summary>
    /// <param name="cancellationToken">A token to observe while committing.</param>
    /// <returns>A task that completes once the commit has finished.</returns>
    /// <remarks>
    /// <see cref="CommitGateDriver"/> only ever calls this once it has guaranteed <see cref="HasActiveTransaction"/>
    /// is <see langword="true"/> - an implementation may assume a transaction is open when this is called.
    /// </remarks>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>Rolls back the session's currently active transaction and disposes it.</summary>
    /// <param name="cancellationToken">A token to observe while rolling back.</param>
    /// <returns>A task that completes once the rollback has finished.</returns>
    /// <remarks>
    /// <see cref="CommitGateDriver"/> only ever calls this once it has guaranteed <see cref="HasActiveTransaction"/>
    /// is <see langword="true"/> - an implementation may assume a transaction is open when this is called.
    /// </remarks>
    Task RollbackAsync(CancellationToken cancellationToken);
}
