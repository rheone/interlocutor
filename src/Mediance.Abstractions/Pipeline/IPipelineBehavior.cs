using Mediance.CommonKernel;

namespace Mediance.Abstractions.Pipeline;

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct);
}
