namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Convenience base class for <see cref="IPipelineStage{TRequest, TResponse}"/> implementations that only
/// need one of the two hooks. Both hooks default to a no-op, so a derived class overrides only the one it needs.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class PipelineStageBase<TRequest, TResponse> : IPipelineStage<TRequest, TResponse>
{
    /// <summary>
    /// Invoked before the request enters the inner pipeline. The default implementation is a no-op that
    /// always continues — override to inspect, short-circuit, or set up state for <see cref="OnAfterAsync"/>.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> resolving to <see cref="StageResult{TResponse}.Continue"/> by default.
    /// </returns>
    public virtual Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct) =>
        Task.FromResult(StageResult<TResponse>.Continue);

    /// <summary>
    /// Invoked after the request has been handled (or short-circuited). The default implementation is a
    /// no-op — override to tear down state set up in <see cref="OnBeforeAsync"/>.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="result">The response: the handler's result, or the short-circuit response if the request was declined.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A completed <see cref="Task"/> by default.</returns>
    public virtual Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct) => Task.CompletedTask;
}
