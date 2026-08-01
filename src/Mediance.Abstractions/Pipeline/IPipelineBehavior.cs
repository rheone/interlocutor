using Mediance.CommonKernel.ResultTypes;

namespace Mediance.Abstractions.Pipeline;

/// <summary>
/// Defines a decorator that intercepts a request before or after its handler executes.
/// </summary>
/// <typeparam name="TRequest">The request type that this behavior processes.</typeparam>
/// <typeparam name="TResponse">The response type returned after pipeline execution.</typeparam>
/// <remarks>
/// <para>
/// Implementations wrap the call to the next handler in the pipeline, enabling
/// cross-cutting concerns such as validation, logging, auditing, transaction
/// management, and authorization to be applied without modifying the handler itself.
/// </para>
/// <para>
/// This follows the decorator and chain-of-responsibility patterns. Behaviors are
/// registered in order and compose around the inner handler delegate.
/// </para>
/// </remarks>
public interface IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Processes the request and invokes the next handler in the pipeline.
    /// </summary>
    /// <param name="request">The incoming request to process.</param>
    /// <param name="next">The next delegate in the pipeline chain to invoke.</param>
    /// <param name="ct">Cancellation token that propagates cancellation notifications.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the handler result or an error if the behavior short-circuits the pipeline.
    /// </returns>
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
