namespace Interlocutor.Abstractions.Queries;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Wraps a query handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TQuery">The concrete query type this wrapper dispatches, constrained to <see cref="IQuery{TResponse}"/> over <typeparamref name="TResponse"/>.</typeparam>
/// <typeparam name="TResponse">The <see cref="IUnion"/>-shaped response type the query's handler produces, chosen by whoever declared <typeparamref name="TQuery"/> - not inherently a "success" shape.</typeparam>
/// <remarks>
/// <para>
/// Resolves the query handler from DI and tags the dispatch activity; <see cref="PipelineChainExecutor"/>
/// composes the pipeline behaviors around it (see that type's remarks for the chain-build/eject-guard logic
/// shared with <c>CommandHandlerWrapper</c> and <c>BareCommandHandlerWrapper</c>). This wrapper is created
/// once per query type and cached by the mediator to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : IHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : IUnion
{
    /// <summary>
    /// Executes the query with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The query to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The <typeparamref name="TResponse"/> produced once the resolved handler and every pipeline behavior around it have run.</returns>
    public Task<TResponse> HandleAsync(
        object request,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    )
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        return PipelineChainExecutor.ExecuteAsync(typedRequest, handler.HandleAsync, services, ct, activity);
    }
}
