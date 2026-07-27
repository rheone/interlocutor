using Mediance.Abstractions;
using Mediance.CommonKernel;
using NHibernate;

namespace Mediance.NHibernate;

public class TransactionBehavior<TRequest, TResponse>(Lazy<ISession> session)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next)
    {
        if (request is not ITransactionalRequest)
            return await next();

        var s = session.Value;
        using var transaction = s.BeginTransaction();

        var result = await next();

        if (result.Value is not Failure)
        {
            await transaction.CommitAsync(ct);
        }
        else
        {
            await transaction.RollbackAsync(ct);
        }

        return result;
    }
}
