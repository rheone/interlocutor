namespace Mediance.Abstractions.Pipeline;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        foreach (var stage in stages)
        {
            await stage.OnBeforeAsync(request, ct);
        }

        TResponse result;
        try
        {
            result = await next();
        }
        catch
        {
            foreach (var stage in stages)
            {
                await stage.OnAfterAsync(request, default, ct);
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
