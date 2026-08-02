namespace Mediance.Abstractions.Queries;

using Mediance.Abstractions.Core;
using Mediance.Abstractions.Pipeline;

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
{
    public override Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

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
