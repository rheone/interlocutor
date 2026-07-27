using Microsoft.EntityFrameworkCore;

namespace Mediance.Mediation.EntityFrameworkCore;

/// <summary>
/// Wraps <see cref="ITransactionalRequest"/> commands in a database
/// transaction, committing once on success or rolling back on any
/// <see cref="Failure"/>. Depends on <c>Lazy&lt;TContext&gt;</c>
/// specifically so requests marked <see cref="IExternalStoreRequest"/>
/// never cause a session to be constructed — <c>db.Value</c> is only
/// evaluated inside the transactional branch.
/// </summary>
/// <typeparam name="TContext">The application's <see cref="DbContext"/> type.</typeparam>
public class TransactionBehavior<TRequest, TResponse, TContext>(Lazy<TContext> db) : IPipelineBehavior<TRequest, TResponse>
    where TContext : DbContext
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (request is not ITransactionalRequest)
            return await next(); // db.Value is never touched for non-transactional requests.

        var context = db.Value;
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var result = await next();

        if (result.Value is not Failure)
        {
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        else
        {
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
