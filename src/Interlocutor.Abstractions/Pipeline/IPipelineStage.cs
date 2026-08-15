namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Lightweight pre/post hook interface without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
/// <typeparam name="TRequest">The request type. Contravariant, so a stage written for a base request type can be registered for any derived request.</typeparam>
/// <typeparam name="TResponse">The response type. Invariant — it appears in the before-hook's return type (<see cref="StageResult{TResponse}"/>), so it cannot be contravariant.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IPipelineStage{TRequest, TResponse}"/> is a simpler alternative to <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// when you only need pre/post hooks without wrapping the handler call. Useful for cross-cutting concerns
/// that only need to observe or occasionally short-circuit a request (like timing, resource cleanup,
/// telemetry, or declining a request without running the handler). Multiple stages can be registered and
/// are invoked via <see cref="PipelineStageBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// <c>OnBeforeAsync</c> runs before the handler; <c>OnAfterAsync</c> runs after, paired with every
/// before-hook that completed — including when a before-hook short-circuits, but not when the handler
/// itself throws. See <see cref="PipelineStageBehavior{TRequest, TResponse}"/> for the full unwind
/// contract. Both hooks default to a no-op via <see cref="PipelineStageBase{TRequest, TResponse}"/>, so
/// an implementer only needs to override the one hook it cares about.
/// </para>
/// </remarks>
public interface IPipelineStage<in TRequest, TResponse>
{
    /// <summary>
    /// Invoked before the request enters the inner pipeline. Returning a short-circuited
    /// <see cref="StageResult{TResponse}"/> skips the handler and any remaining before-hooks; the
    /// short-circuit response then unwinds through <c>OnAfterAsync</c> for every stage whose before-hook
    /// already ran, including this one, before flowing out as an ordinary response.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> resolving to <see cref="StageResult{TResponse}.Continue"/> to let the
    /// request proceed, or a value produced by <see cref="StageResult{TResponse}.ShortCircuit(TResponse)"/>
    /// to decline it.
    /// </returns>
    Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>
    /// Invoked after the request has been handled and a result is available — either the handler's result,
    /// or the response a before-hook short-circuited with. Not invoked when the handler throws.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="result">
    /// The response: the handler's result, or the short-circuit response if the request was declined.
    /// <b>Exception: if a <em>later</em> stage's <c>OnBeforeAsync</c> throws</b> — not this stage's own — the
    /// exception propagates past every earlier stage's before-hook and the resulting unwind still calls each
    /// of their <c>OnAfterAsync</c> hooks (so setup/teardown pairs still run), but with no real response to
    /// hand them: <paramref name="result"/> is <see langword="default"/> in that case, not a genuine outcome.
    /// A stage whose teardown logic reads <paramref name="result"/> should treat a plausible-looking default
    /// value received here as suspect rather than trust it unconditionally — there is no in-band signal on
    /// this parameter distinguishing "genuinely defaulted response" from "before-hook exception, no response
    /// exists." See <see cref="PipelineStageBehavior{TRequest, TResponse}"/> for the full unwind contract.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct);
}
