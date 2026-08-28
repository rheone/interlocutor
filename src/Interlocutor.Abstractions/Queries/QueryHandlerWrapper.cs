namespace Interlocutor.Abstractions.Queries;

using System.Diagnostics;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Wraps a query handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
/// <remarks>
/// <para>
/// Resolves the query handler and all applicable pipeline behaviors from DI, then composes them into a chain
/// in reverse registration order (innermost-to-outermost). This wrapper is created once per query type and cached
/// by the mediator to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : HandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IOperationOutcome
{
    /// <summary>
    /// Executes the query with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The query to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The response from the handler.</returns>
    public override Task<TResponse> Handle(
        object request,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    )
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        // Start with the innermost pipeline delegate pointing to the handler itself.
        var pipeline = () => handler.Handle(typedRequest, ct);

        // Wrap the handler with each behavior in reverse registration order so the first-registered behavior
        // runs first (inbound) and last (outbound), creating an onion-like structure. Each behavior wraps
        // the previous pipeline, capturing it in a closure.
        foreach (var behavior in services.GetServices<IPipelineBehavior<TQuery, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline();
    }
}
