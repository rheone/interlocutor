namespace Interlocutor.Abstractions.Pipeline;

/// <summary>Lightweight pre/post hook interface without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IPipelineStage{TRequest, TResponse}"/> is a simpler alternative to <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// when you only need pre/post hooks without wrapping the handler call. Useful for cross-cutting concerns that
/// don't short-circuit (like timing, resource cleanup, or telemetry). Multiple stages can be registered and are
/// invoked via <see cref="PipelineStageBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// <c>OnBeforeAsync</c> runs before the handler; <c>OnAfterAsync</c> runs after (even on failure). This lets you
/// implement paired resource allocation/cleanup (e.g., open/close a connection).
/// </para>
/// </remarks>
public interface IPipelineStage<TRequest, TResponse>
{
    /// <summary>Invoked before the request enters the inner pipeline.</summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>Invoked after the request has been handled and the result is available, or after the handler threw an exception.</summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="result">The result from handling the request. If the handler or a prior stage threw an exception, this value is zero-initialized (not null).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct);
}
