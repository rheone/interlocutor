namespace Mediance.Abstractions.Pipeline;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class PipelineStageBehavior<TRequest, TResponse>(IEnumerable<IPipelineStage<TRequest, TResponse>> stages)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request, invoking registered pipeline stages around the handler call.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result from the next behavior or handler.</returns>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var stagesList = stages.ToList();
        var completedStages = new List<int>();

        // Invoke all pre-handler hooks in order. Stages run in registration order; if any hook throws,
        // subsequent stages' OnBefore methods are not invoked. Track which stages completed so their
        // OnAfter hooks run in reverse order below, regardless of how the pipeline exits.
        try
        {
            for (int i = 0; i < stagesList.Count; i++)
            {
                await stagesList[i].OnBeforeAsync(request, ct);
                completedStages.Add(i);
            }
        }
        catch
        {
            // OnBefore failed. Run OnAfter for completed stages in reverse order to unwind resources,
            // then re-throw the original exception. Even if OnAfter fails, the original OnBefore
            // exception takes precedence.
            try
            {
                await UnwindStages(stagesList, completedStages, request, default, ct);
            }
            catch
            {
                // Suppress OnAfter exception; re-throw original OnBefore exception.
            }

            throw;
        }

        TResponse result;
        try
        {
            result = await next();
        }
        catch
        {
            // Handler threw an exception. Run OnAfter hooks for completed stages in reverse order to ensure
            // cleanup happens (e.g., close connections). The response is zero-initialized since no handler result was produced.
            // Even if OnAfter fails, the original handler exception takes precedence.
            try
            {
                await UnwindStages(stagesList, completedStages, request, default, ct);
            }
            catch
            {
                // Suppress OnAfter exception; re-throw original handler exception.
            }

            throw;
        }

        // Handler succeeded. Run OnAfter hooks for completed stages in reverse order with the actual result.
        // If OnAfter fails here (not masking a prior exception), let the exception propagate.
        await UnwindStages(stagesList, completedStages, request, result, ct);

        return result;
    }

    /// <summary>
    /// Invokes the OnAfter hook for each stage whose OnBefore completed, in reverse order.
    /// </summary>
    private static async Task UnwindStages(
        List<IPipelineStage<TRequest, TResponse>> stagesList,
        List<int> completedStages,
        TRequest request,
        TResponse response,
        CancellationToken ct)
    {
        // Run OnAfter hooks in reverse order of completion to unwind resource acquisition.
        for (int i = completedStages.Count - 1; i >= 0; i--)
        {
            await stagesList[completedStages[i]].OnAfterAsync(request, response, ct);
        }
    }
}
