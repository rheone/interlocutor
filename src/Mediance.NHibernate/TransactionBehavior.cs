using Mediance.Abstractions;
using Mediance.Abstractions.Markers;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;
using NHibernate;

namespace Mediance.NHibernate;

public class TransactionBehavior<TRequest, TResponse>(Lazy<ISession> session) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        if (request is not ITransactionalRequest || request is IExternalStoreRequest)
        {
            return await next();
        }

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
