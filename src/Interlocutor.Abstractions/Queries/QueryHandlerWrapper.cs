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
/// Resolves the query handler from DI, resolves the applicable pipeline behavior list via
/// <see cref="PipelineChainResolver"/> - the general-purpose baseline, or a matched per-request-type
/// override's declared list - then composes them into a chain in reverse order (innermost-to-outermost).
/// This wrapper is created once per query type and cached by the mediator to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : IHandlerWrapper<TResponse>
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
    public Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null)
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        // Start with the innermost pipeline delegate pointing to the handler itself. Wrapped in a try/catch
        // for PipelineEjectException so a handler that calls Eject.With deep in its own call graph returns an
        // ordinary response here - nothing further out in the chain ever observes a thrown exception.
        Func<Task<TResponse>> pipeline = async () =>
        {
            try
            {
                return await handler.Handle(typedRequest, ct);
            }
            catch (PipelineEjectException<TResponse> eject)
            {
                return eject.Response;
            }
        };

        // Wrap the handler with each behavior in reverse order (baseline registration order, or a matched
        // override's declared order - see PipelineChainResolver) so the first entry runs first (inbound) and
        // last (outbound), creating an onion-like structure. Each behavior wraps the previous pipeline,
        // capturing it in a closure. Each wrap gets the same PipelineEjectException catch, so a behavior that
        // ejects - before or after calling next() - unwinds no further than its own caller in this chain.
        foreach (var behavior in PipelineChainResolver.ResolveChain<TQuery, TResponse>(services).Reverse())
        {
            var next = pipeline;
            pipeline = async () =>
            {
                try
                {
                    return await behavior.Handle(typedRequest, next, ct);
                }
                catch (PipelineEjectException<TResponse> eject)
                {
                    return eject.Response;
                }
            };
        }

        return pipeline();
    }
}
