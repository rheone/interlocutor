namespace Interlocutor.Persistence.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// An EF Core <see cref="SaveChangesInterceptor"/> that opens a transaction on the <see cref="DbContext"/>
/// before any <c>SaveChanges</c>/<c>SaveChangesAsync</c> call reaches the database, if one is not already open.
/// </summary>
/// <remarks>
/// <para>
/// EF Core's begin-on-first-write guarantee falls out for free, because nothing reaches the database before
/// <c>SaveChanges</c> runs, regardless of how entities were staged - wrapping that one call catches every
/// write uniformly. This interceptor is that wrap: it fires for <em>every</em> call to the context's
/// <c>SaveChanges</c>/<c>SaveChangesAsync</c>, whichever code path made it - <see cref="EntityFrameworkUnitOfWork{TContext}"/>,
/// an <c>IDomainRepository</c> consumer that also calls it, or a handler holding the context directly via a
/// raw <c>Lazy&lt;TContext&gt;</c> injection - so the whole-session protection
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> provides for the blessed access paths also
/// covers that last, fully-direct one, matching the retired <c>TransactionBehavior</c>'s previous whole-session
/// guarantee rather than regressing to abstraction-only protection.
/// </para>
/// <para>
/// Not itself DI-resolved: it holds no per-dispatch state (it reads and mutates only the
/// <see cref="DbContext"/> instance <c>SavingChangesAsync</c> is handed), so a single instance is safe to
/// share across every scope and every <c>DbContext</c> type. Register it once, statelessly, on the
/// application's own <see cref="DbContextOptionsBuilder"/> - for example
/// <c>options.AddInterceptors(new UnitOfWorkSaveChangesInterceptor())</c> inside the <c>AddDbContext</c> call
/// that configures the application's <see cref="DbContext"/> - alongside registering
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> in the pipeline. Without this interceptor
/// wired up, only the <see cref="EntityFrameworkUnitOfWork{TContext}"/> façade and
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> itself still protect writes; a handler that
/// calls a raw, directly-injected <c>Lazy&lt;TContext&gt;</c>'s own <c>SaveChangesAsync</c> mid-handler is not
/// covered.
/// </para>
/// <para>
/// Suppressed entirely for a dispatch whose request implements <c>IExternalStoreRequest</c> -
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> marks that suppression for the duration of
/// the handler call so this interceptor never opens a local transaction around a write that request
/// explicitly said belongs to an external store instead.
/// </para>
/// </remarks>
public sealed class UnitOfWorkSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Flows suppression across the async call chain of a single dispatch - set by
    /// <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/> around an <c>IExternalStoreRequest</c>
    /// handler's execution, read here to skip opening a transaction for that dispatch's <c>SaveChanges</c> calls.
    /// </summary>
    internal static readonly AsyncLocal<bool> IsSuppressed = new();

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsSuppressed.Value && eventData.Context is { } context && context.Database.CurrentTransaction is null)
        {
            // Not captured/disposed here - EntityFrameworkCommitGateSession's CommitAsync/RollbackAsync retrieve
            // this same context-owned transaction via Database.CurrentTransaction and dispose it once, after
            // commit or rollback, at pipeline completion.
#pragma warning disable IDISP004
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore IDISP004
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>Covers the synchronous <see cref="DbContext.SaveChanges()"/> call path, for parity with <see cref="SavingChangesAsync"/>.</remarks>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (!IsSuppressed.Value && eventData.Context is { } context && context.Database.CurrentTransaction is null)
        {
            // Not captured/disposed here - EntityFrameworkCommitGateSession's CommitAsync/RollbackAsync retrieve
            // this same context-owned transaction via Database.CurrentTransaction and dispose it once, after
            // commit or rollback, at pipeline completion.
#pragma warning disable IDISP004
            context.Database.BeginTransaction();
#pragma warning restore IDISP004
        }

        return base.SavingChanges(eventData, result);
    }
}
