namespace Mediance.Abstractions.Core;

using Mediance.CommonKernel;

/// <summary>Defines the base contract for handlers that produce an outcome-only (no data payload) result.</summary>
/// <typeparam name="TResult">The <see cref="IOperationOutcome"/>-shaped outcome type produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// This is an internal abstraction used by the mediator to wrap void command handlers. Concrete subclasses
/// (e.g., <c>VoidCommandHandlerWrapper</c>) construct the pipeline behavior chain at invocation time.
/// </para>
/// </remarks>
internal abstract class VoidHandlerWrapper<TResult>
    where TResult : IOperationOutcome
{
    /// <summary>
    /// Executes the handler with the given request, resolving behaviors and the handler from the service provider.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="services">The service provider for resolving handlers and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The outcome from the handler.</returns>
    public abstract Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct);
}
