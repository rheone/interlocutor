using Mediance.Abstractions;
using Mediance.CommonKernel;
using Microsoft.EntityFrameworkCore;

namespace Mediance.EntityFrameworkCore;

public class TransactionBehavior<TRequest, TResponse, TContext>(Lazy<TContext> db) : IPipelineBehavior<TRequest, TResponse>
    where TContext : DbContext
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not ITransactionalRequest)
        {
            return await next();
        }

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
