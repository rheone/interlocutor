namespace Interlocutor.Persistence.NHibernate;

using global::NHibernate;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// Adapts an NHibernate <see cref="ISession"/> to the <see cref="ICommitGateSession"/> seam
/// <see cref="CommitGateDriver"/> drives, so <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> only needs
/// to hand the driver this adapter instead of re-implementing the begin/commit/rollback control flow itself.
/// </summary>
/// <remarks>
/// <para>
/// Constructing this adapter sets the wrapped session's <see cref="ISession.FlushMode"/> to
/// <see cref="FlushMode.Commit"/>. NHibernate's default flush mode can auto-flush pending changes before a
/// query executes, which could write to the database ahead of any transaction the driver opens around it -
/// setting this at construction, before the driver runs the next pipeline delegate, closes that window. See
/// <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>'s remarks for the full rationale.
/// </para>
/// <para>
/// <see cref="CommitAsync"/> and <see cref="RollbackAsync"/> both retrieve the session's current transaction
/// via <see cref="SessionExtensions.GetCurrentTransaction(ISession)"/> rather than holding onto the
/// <see cref="ITransaction"/> instance <see cref="BeginTransactionAsync"/> begins - <see cref="ICommitGateSession"/>
/// guarantees <see cref="CommitGateDriver"/> never calls either without first guaranteeing
/// <see cref="HasActiveTransaction"/>, so the transaction retrieved here is always the one already open.
/// </para>
/// </remarks>
internal sealed class NHibernateCommitGateSession : ICommitGateSession
{
    private readonly ISession session;

    /// <summary>Initializes a new instance of the <see cref="NHibernateCommitGateSession"/> class, priming <paramref name="session"/>'s flush mode.</summary>
    /// <param name="session">The scoped session this adapter wraps for the current dispatch.</param>
    public NHibernateCommitGateSession(ISession session)
    {
        // Prevents NHibernate's default auto-flush-before-query from writing staged changes ahead of any
        // transaction the driver opens - see this type's remarks.
        session.FlushMode = FlushMode.Commit;
        this.session = session;
    }

    /// <inheritdoc/>
    public bool HasActiveTransaction => this.session.GetCurrentTransaction() is { IsActive: true };

    /// <inheritdoc/>
    public async Task<bool> HasPendingWorkAsync(CancellationToken cancellationToken) =>
        await this.session.IsDirtyAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        // Not captured/disposed here - CommitAsync/RollbackAsync retrieve this same session-owned transaction
        // via GetCurrentTransaction() and dispose it once, after committing or rolling back.
#pragma warning disable IDISP004
        this.session.BeginTransaction();
#pragma warning restore IDISP004
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        // CommitGateDriver only calls this once it has guaranteed a transaction is open - see this type's
        // remarks - so GetCurrentTransaction() is never null here.
        using var transaction = this.session.GetCurrentTransaction()!;
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        // CommitGateDriver only calls this once it has guaranteed a transaction is open - see this type's
        // remarks - so GetCurrentTransaction() is never null here.
        using var transaction = this.session.GetCurrentTransaction()!;
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }
}
