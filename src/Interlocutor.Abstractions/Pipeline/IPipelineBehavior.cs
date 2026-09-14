namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Defines a decorator that intercepts a request before or after its handler executes.
/// </summary>
/// <typeparam name="TRequest">The request type that this behavior processes.</typeparam>
/// <typeparam name="TResponse">
/// The response type produced for <typeparamref name="TRequest"/>. This interface places no constraint on
/// <typeparamref name="TResponse"/> itself - it is whatever type the handler for <typeparamref name="TRequest"/>
/// returns, chosen by whoever declared that request. A concrete behavior that needs to construct or recognize
/// a specific outcome case (for example, short-circuiting with a <c>Forbidden</c> or <c>Timeout</c> value)
/// narrows <typeparamref name="TResponse"/> further with its own <c>where TResponse : IUnion</c> constraint;
/// this base interface does not require that.
/// </typeparam>
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
    /// A task that represents the asynchronous operation. The task result is either the value the inner
    /// handler produced, or a value this behavior (or one further down the chain) substituted in its place
    /// to short-circuit the pipeline without calling the handler at all.
    /// </returns>
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
