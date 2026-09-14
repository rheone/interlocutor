namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Template-method convenience base class for <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// implementations that need a before/after split around the inner handler call, with the ability to
/// replace the response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">
/// The response type produced for <typeparamref name="TRequest"/>, threaded unchanged through
/// <see cref="OnEnterAsync"/> and <see cref="OnExitAsync"/>. Like <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// itself, this base class places no constraint on it - it is whatever type the request's handler returns; a
/// derived class that needs to construct a specific outcome case adds its own constraint (typically
/// <c>where TResponse : IUnion</c>) rather than relying on one declared here.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="HandleAsync"/> is implemented once, as a fixed template, and is not itself an extension
/// point - a derived class overrides only <see cref="OnEnterAsync"/> and/or <see cref="OnExitAsync"/>, both
/// of which default to a no-op, so overriding one leaves the other a pure pass-through.
/// </para>
/// <para>
/// Choosing between this class and <see cref="IPipelineStage{TRequest, TResponse}"/> (conveniently via
/// <see cref="PipelineStageBase{TRequest, TResponse}"/>) comes down to whether the cross-cutting concern
/// owns the response. Reach for <see cref="IPipelineStage{TRequest, TResponse}"/> for a non-mutating
/// observer meant to compose with other stages via <see cref="PipelineStageBehavior{TRequest, TResponse}"/>
/// - its after-hook can only observe the result, not change it, which is what lets arbitrarily many stages
/// share one host safely. Reach for <see cref="PipelineBehaviorBase{TRequest, TResponse}"/> for a single
/// behavior that needs to veto or replace the response outright - for example, one managing a transactional
/// resource that decides, after the handler runs, to commit, roll back, or swap in a different response -
/// without throwing an exception to do it.
/// </para>
/// <para>
/// <see cref="OnExitAsync"/> runs whenever a response exists: either <c>next()</c> completed normally, or
/// <see cref="OnEnterAsync"/> short-circuited it. It never runs when <c>next()</c> throws - the exception
/// propagates out of <see cref="HandleAsync"/> untouched, matching <see cref="IPipelineStage{TRequest, TResponse}"/>'s
/// existing after-hook contract (see <see cref="PipelineStageBehavior{TRequest, TResponse}"/>'s remarks) and
/// the shipped commit-gate behavior's actual design: an unhandled exception is never caught to decide
/// rollback, it propagates untouched and leaves any open transaction for the underlying store session's own
/// disposal to roll back.
/// </para>
/// </remarks>
public abstract class PipelineBehaviorBase<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Processes the request: calls <see cref="OnEnterAsync"/>, skips <paramref name="next"/> and uses the
    /// short-circuited response if that hook declined the call, otherwise awaits <paramref name="next"/>,
    /// then always calls <see cref="OnExitAsync"/> with whatever response resulted. This method is the fixed
    /// template - it is not an extension point; override <see cref="OnEnterAsync"/> and/or
    /// <see cref="OnExitAsync"/> instead.
    /// </summary>
    /// <param name="request">The incoming request to process.</param>
    /// <param name="next">The next delegate in the pipeline chain to invoke.</param>
    /// <param name="ct">Cancellation token that propagates cancellation notifications.</param>
    /// <returns>
    /// <see cref="OnExitAsync"/>'s result - by default, whichever response resulted from <paramref name="next"/>
    /// or an <see cref="OnEnterAsync"/> short-circuit, unchanged. Never resolves at all if
    /// <paramref name="next"/> throws - the exception propagates out of this method instead.
    /// </returns>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var enterResult = await this.OnEnterAsync(request, ct).ConfigureAwait(false);
        var result = enterResult.ShortCircuited ? enterResult.Response : await next().ConfigureAwait(false);
        return await this.OnExitAsync(request, result, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Invoked before the request reaches <c>next()</c>. The default implementation is a no-op that always
    /// continues - override to inspect the request, short-circuit it without running the handler, or set up
    /// state for <see cref="OnExitAsync"/>.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> resolving to <see cref="StageResult{TResponse}.Continue"/> to let
    /// <c>next()</c> run, or a value produced by <see cref="StageResult{TResponse}.ShortCircuit(TResponse)"/>
    /// to skip it and hand <see cref="OnExitAsync"/> the short-circuit response directly.
    /// </returns>
    protected virtual Task<StageResult<TResponse>> OnEnterAsync(TRequest request, CancellationToken ct) =>
        Task.FromResult(StageResult<TResponse>.Continue);

    /// <summary>
    /// Invoked after a response is available - either <c>next()</c>'s result, or the response
    /// <see cref="OnEnterAsync"/> short-circuited with. Not invoked when <c>next()</c> throws. Unlike
    /// <see cref="IPipelineStage{TRequest, TResponse}.OnAfterAsync"/>, this hook returns
    /// <typeparamref name="TResponse"/>, so it may replace the response rather than only observe it. The
    /// default implementation returns <paramref name="result"/> unchanged - override to veto, replace, or
    /// tear down state set up in <see cref="OnEnterAsync"/>.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="result">The response: <c>next()</c>'s result, or the short-circuit response if <see cref="OnEnterAsync"/> declined the call.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> resolving to the response <see cref="HandleAsync"/> returns - <paramref name="result"/>, by default.</returns>
    protected virtual Task<TResponse> OnExitAsync(TRequest request, TResponse result, CancellationToken ct) =>
        Task.FromResult(result);
}
