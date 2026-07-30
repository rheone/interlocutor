using Mediance.CommonKernel;

namespace Mediance.Abstractions.Pipeline;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct)
    {
        foreach (var stage in stages)
        {
            await stage.OnBeforeAsync(request, ct);
        }

        Result<TResponse> result;
        try
        {
            result = await next();
        }
        catch
        {
            foreach (var stage in stages)
            {
                await stage.OnAfterAsync(request, default(Result<TResponse>), ct);
            }

            throw;
        }

        foreach (var stage in stages)
        {
            await stage.OnAfterAsync(request, result, ct);
        }

        return result;
    }
}
