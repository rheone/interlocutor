namespace Interlocutor.Persistence.EntityFrameworkCore;

using Interlocutor.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The EF Core <see cref="IUnitOfWork"/> implementation - the transaction owner for the scoped
/// <typeparamref name="TContext"/> for the current dispatch.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type providing database access.</typeparam>
/// <param name="context">
/// Lazy factory for the <typeparamref name="TContext"/> instance, matching <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s
/// own <c>Lazy&lt;TContext&gt;</c> idiom so a dispatch that never touches this unit of work never pays for
/// context construction.
/// </param>
/// <remarks>
/// Registered scoped, matching <typeparamref name="TContext"/>'s own registration lifetime, so this unit of
/// work shares the same scoped context - and the same transaction - that
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> commits or rolls back at pipeline completion.
/// </remarks>
public sealed class EntityFrameworkUnitOfWork<TContext>(Lazy<TContext> context) : IUnitOfWork
    where TContext : DbContext
{
    /// <inheritdoc/>
    /// <remarks>
    /// Begins a transaction first if one is not already open - covering the case where this method is the
    /// first write of the dispatch, called before <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>
    /// or <c>UnitOfWorkSaveChangesInterceptor</c> would otherwise have noticed - then forwards to
    /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>. The transaction this opens stays open
    /// beyond this call's return; it is committed or rolled back by the commit-gate behavior at pipeline
    /// completion, not by this method.
    /// </remarks>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var db = context.Value;
        if (db.Database.CurrentTransaction is null)
        {
            // Not captured/disposed here - EntityFrameworkCommitGateSession's CommitAsync/RollbackAsync retrieve
            // this same context-owned transaction via Database.CurrentTransaction and dispose it once, after
            // commit or rollback, at pipeline completion.
#pragma warning disable IDISP004
            await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore IDISP004
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Rolls back and disposes <c>Database.CurrentTransaction</c> if one is open, then clears the change
    /// tracker (<c>ChangeTracker.Clear()</c>) so nothing staged during this dispatch remains pending - the
    /// commit-gate behavior's end-of-pipeline check finds no open transaction and no pending changes, and
    /// does nothing further. A no-op if no transaction is open.
    /// </remarks>
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var db = context.Value;
        if (db.Database.CurrentTransaction is { } transaction)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await transaction.DisposeAsync().ConfigureAwait(false);
        }

        db.ChangeTracker.Clear();
    }
}
