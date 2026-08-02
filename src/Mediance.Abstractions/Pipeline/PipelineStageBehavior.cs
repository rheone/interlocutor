namespace Mediance.Abstractions.Pipeline;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Invoke all pre-handler hooks. Stages run in registration order; if any hook throws, the exception
        // propagates and subsequent stages' OnBefore methods are not invoked. Paired with OnAfter below
        // for resource allocation/setup.
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
            // Handler threw an exception. Run OnAfter hooks anyway to ensure cleanup happens (e.g., close connections).
            // Pass default result since no response was produced.
            foreach (var stage in stages)
            {
                await stage.OnAfterAsync(request, default, ct);
            }

            throw;
        }

        // Handler succeeded. Run OnAfter hooks with the actual result. This runs in reverse order if stages
        // are treated as a stack, though here we iterate forward for simplicity.
        foreach (var stage in stages)
        {
            await stage.OnAfterAsync(request, result, ct);
        }

        return result;
    }
}
