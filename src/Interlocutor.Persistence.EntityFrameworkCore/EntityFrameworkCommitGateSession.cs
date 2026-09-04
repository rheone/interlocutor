namespace Interlocutor.Persistence.EntityFrameworkCore;

using Interlocutor.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Adapts an EF Core <see cref="DbContext"/> to the <see cref="ICommitGateSession"/> seam
/// <see cref="CommitGateDriver"/> drives, so <see cref="UnitOfWorkBehavior{TRequest, TResponse, TContext}"/>
/// only needs to hand the driver this adapter instead of re-implementing the begin/commit/rollback control
/// flow itself.
/// </summary>
/// <remarks>
/// <para>
/// Unlike NHibernate, committing an EF Core transaction does not implicitly persist tracked changes -
/// <see cref="CommitAsync"/> therefore calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> before
/// committing the transaction (a no-op if a handler already called it mid-handler), rolling back and
/// rethrowing if the save itself fails rather than committing a transaction whose intended writes never
/// reached the database.
/// </para>
/// <para>
/// <see cref="CommitAsync"/> and <see cref="RollbackAsync"/> both retrieve the context's current transaction
/// via <c>Database.CurrentTransaction</c> rather than holding onto the transaction instance
/// <see cref="BeginTransactionAsync"/> begins - <see cref="ICommitGateSession"/> guarantees
/// <see cref="CommitGateDriver"/> never calls either without first guaranteeing <see cref="HasActiveTransaction"/>,
/// so the transaction retrieved here is always the one already open.
/// </para>
/// </remarks>
internal sealed class EntityFrameworkCommitGateSession(DbContext context) : ICommitGateSession
{
    /// <inheritdoc/>
    public bool HasActiveTransaction => context.Database.CurrentTransaction is not null;

    /// <inheritdoc/>
    public Task<bool> HasPendingWorkAsync(CancellationToken cancellationToken) =>
        Task.FromResult(context.ChangeTracker.HasChanges());

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        // Not captured/disposed here - CommitAsync/RollbackAsync retrieve this same context-owned transaction
        // via Database.CurrentTransaction and dispose it once, after committing or rolling back.
#pragma warning disable IDISP004
        await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore IDISP004
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        // CommitGateDriver only calls this once it has guaranteed a transaction is open - see this type's
        // remarks - so CurrentTransaction is never null here.
        //
        // IDisposableAnalyzers treats the context field as "injected" and flags disposing anything retrieved
        // from it (IDISP007); that heuristic doesn't see the actual ownership here - CommitGateDriver
        // guarantees this transaction was opened for this dispatch alone, and this is the sole place it is
        // committed/rolled back and disposed.
#pragma warning disable IDISP007
        var transaction = context.Database.CurrentTransaction!;
        await using var scopedTransaction = transaction;
#pragma warning restore IDISP007

        try
        {
            // Persists whatever is still staged - a handler that already called SaveChangesAsync mid-handler
            // leaves nothing pending here, so this is a no-op in that case.
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        // CommitGateDriver only calls this once it has guaranteed a transaction is open - see this type's
        // remarks - so CurrentTransaction is never null here. See CommitAsync for why IDISP007 is suppressed.
#pragma warning disable IDISP007
        var transaction = context.Database.CurrentTransaction!;
        await using var scopedTransaction = transaction;
#pragma warning restore IDISP007
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }
}
