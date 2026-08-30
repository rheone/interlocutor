namespace Interlocutor.Abstractions.Pipeline;

using Microsoft.Extensions.Logging;

/// <summary>Aggregates all registered <see cref="IPipelineStage{TRequest,TResponse}"/> instances and invokes them around the handler call.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="stages">The registered stages, invoked in this enumeration's order.</param>
/// <param name="logger">Used to log an after-hook exception that would otherwise be silently discarded during unwind.</param>
/// <param name="rethrowAfterHookExceptions">
/// When <see langword="true"/>, an after-hook exception is logged and then re-thrown, aborting the
/// remainder of the unwind and propagating out of <see cref="Handle"/>. When <see langword="false"/>
/// (the default), it is logged and swallowed, and unwind continues with the next stage - so a failing
/// teardown hook can never mask the pipeline's real result.
/// </param>
/// <remarks>
/// An ordinary entry in the pipeline behavior list - the developer positions it anywhere and may register
/// it more than once. It is not pinned relative to any other behavior (for example, a transaction
/// behavior); see ADR-0002's amendment for the full short-circuit and unwind contract this class implements.
/// </remarks>
public class PipelineStageBehavior<TRequest, TResponse>(
    IEnumerable<IPipelineStage<TRequest, TResponse>> stages,
    ILogger<PipelineStageBehavior<TRequest, TResponse>> logger,
    bool rethrowAfterHookExceptions = false
) : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request, invoking registered pipeline stages around the handler call.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The result from the next behavior or handler, or - if a before-hook short-circuited - the
    /// short-circuit response. Both cases return through this same <see cref="Task{TResult}"/>, so
    /// behaviors outside this host cannot tell them apart.
    /// </returns>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var stagesList = stages.ToList();
        var completedStages = new List<int>();

        // Invoke before-hooks in order. A stage is "completed" once its OnBefore call returns without
        // throwing, regardless of whether it short-circuited -- completedStages drives which stages
        // receive OnAfter, and in what (reverse) order, however the pipeline exits below.
        for (var i = 0; i < stagesList.Count; i++)
        {
            StageResult<TResponse> beforeResult;
            try
            {
                beforeResult = await stagesList[i].OnBeforeAsync(request, ct).ConfigureAwait(false);
            }
            catch
            {
                // OnBefore threw. Unwind After for stages that already completed (not this one -- it
                // never finished), then let the original exception propagate. Even if unwind itself
                // throws (only possible when rethrowAfterHookExceptions is set), the original exception
                // still wins per UnwindStagesAsync's own contract... except when the rethrow escalation
                // is explicitly requested, in which case the caller has opted into losing that guarantee.
                await this.UnwindStagesAsync(stagesList, completedStages, request, default!, ct).ConfigureAwait(false);
                throw;
            }

            completedStages.Add(i);

            if (beforeResult.ShortCircuited)
            {
                // Short-circuit: remaining before-hooks and the handler are both skipped. Unwind After in
                // reverse for exactly the stages whose Before ran -- including this one -- with the
                // short-circuit response, then return it as an ordinary result so behaviors outside this
                // host cannot distinguish it from a handled response.
                await this.UnwindStagesAsync(stagesList, completedStages, request, beforeResult.Response, ct)
                    .ConfigureAwait(false);
                return beforeResult.Response;
            }
        }

        // No before-hook short-circuited; run the handler. An exception here is a bug or infrastructure
        // fault, not an expected outcome -- OnAfter is paired with a produced response and deliberately
        // does not run, so a stage cannot observe a response that was never produced.
        var result = await next().ConfigureAwait(false);

        // Handler succeeded. Run OnAfter hooks for completed stages in reverse order with the actual result.
        await this.UnwindStagesAsync(stagesList, completedStages, request, result, ct).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Invokes the OnAfter hook for each stage whose OnBefore completed, in reverse order. An exception
    /// from one stage's OnAfter is logged and, unless the constructor's <c>rethrowAfterHookExceptions</c>
    /// switch was set to re-throw, swallowed so the remaining stages still unwind.
    /// </summary>
    private async Task UnwindStagesAsync(
        List<IPipelineStage<TRequest, TResponse>> stagesList,
        List<int> completedStages,
        TRequest request,
        TResponse response,
        CancellationToken ct
    )
    {
        // Run OnAfter hooks in reverse order of completion to unwind resource acquisition.
        for (var i = completedStages.Count - 1; i >= 0; i--)
        {
            var stage = stagesList[completedStages[i]];
            try
            {
                await stage.OnAfterAsync(request, response, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Pipeline stage {StageType} threw from OnAfterAsync while processing request {RequestType}",
                    stage.GetType(),
                    typeof(TRequest)
                );

                if (rethrowAfterHookExceptions)
                {
                    throw;
                }
            }
        }
    }
}
