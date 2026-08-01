namespace Mediance.Abstractions.Pipeline;

/// <summary>Lightweight pre/post hook interface without creating a full <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</summary>
public interface IPipelineStage<TRequest, TResponse>
{
    /// <summary>Invoked before the request enters the inner pipeline.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnBeforeAsync(TRequest request, CancellationToken ct);

    /// <summary>Invoked after the request has been handled and the result is available.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnAfterAsync(TRequest request, Result<TResponse> result, CancellationToken ct);
}
