namespace Mediance.Abstractions.Pipeline;

/// <summary>Lightweight pre/post hook interface without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineStage<TRequest, TResponse>
{
    /// <summary>Invoked before the request enters the inner pipeline.</summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>Invoked after the request has been handled and the result is available.</summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="result">The result from handling the request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnAfterAsync(TRequest request, TResponse? result, CancellationToken ct);
}
