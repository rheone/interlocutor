using Microsoft.EntityFrameworkCore;

namespace Interlocutor.Mediation.EntityFrameworkCore;

/// <summary>
/// Commits or rolls back whatever transaction a dispatch opened. Unlike an earlier design where this
/// behavior itself opened the transaction for every <see cref="ITransactionalRequest"/>, <see cref="IUnitOfWork"/>
/// is the transaction owner here: it opens lazily, the first time anything actually writes - through
/// <c>IDomainRepository</c>, through <see cref="IUnitOfWork"/> itself, or through this same directly-injected
/// <c>Lazy&lt;TContext&gt;</c> - so a dispatch that only reads never pays for an open transaction. No marker
/// is required to participate; <see cref="ITransactionalRequest"/> narrows to forcing that begin to happen
/// eagerly, at pipeline start, for the check-then-act case where a protective read must share the same
/// transaction as the write that follows it.
/// </summary>
/// <typeparam name="TContext">The application's <see cref="DbContext"/> type.</typeparam>
public class UnitOfWorkBehavior<TRequest, TResponse, TContext>(Lazy<TContext> db) : IPipelineBehavior<TRequest, TResponse>
    where TContext : DbContext
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (request is IExternalStoreRequest)
        {
            return await next(); // Never touches db - this request manages its own external transaction.
        }

        var context = db.Value;

        if (request is ITransactionalRequest && context.Database.CurrentTransaction is null)
        {
            await context.Database.BeginTransactionAsync(ct); // Eager begin, before any read the handler runs.
        }

        var result = await next();

        // Nothing to finalize if nothing wrote: no transaction is open and nothing is staged - the common
        // case for a pure read, and the reason this behavior never opens one for that case on its own.
        if (context.Database.CurrentTransaction is null && !context.ChangeTracker.HasChanges())
        {
            return result;
        }

        var transaction = context.Database.CurrentTransaction ?? await context.Database.BeginTransactionAsync(ct);
        await using var scopedTransaction = transaction;

        if (result.Value is not Failure)
        {
            await context.SaveChangesAsync(ct); // A no-op if the handler already called IUnitOfWork.SaveChangesAsync.
            await transaction.CommitAsync(ct);
        }
        else
        {
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
