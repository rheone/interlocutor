namespace Interlocutor.Abstractions.Commands;

using System.Diagnostics;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Wraps a value-returning command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
/// <remarks>
/// <para>
/// Resolves the command handler from DI, resolves the applicable pipeline behavior list via
/// <see cref="PipelineChainResolver"/> - the general-purpose baseline, or a matched per-request-type
/// override's declared list - then composes them into a chain in reverse order (so the first entry runs
/// first inbound / last outbound). This wrapper is created once per command type and cached by the mediator
/// to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : IHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IOperationOutcome
{
    /// <summary>
    /// Executes the command with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The response from the handler.</returns>
    public Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        // Start with the innermost pipeline delegate pointing to the handler itself. Wrapped in a try/catch
        // for PipelineEjectException so a handler that calls Eject.With deep in its own call graph returns an
        // ordinary response here - nothing further out in the chain ever observes a thrown exception.
        Func<Task<TResponse>> pipeline = async () =>
        {
            try
            {
                return await handler.Handle(typedRequest, ct).ConfigureAwait(false);
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
        foreach (var behavior in PipelineChainResolver.ResolveChain<TCommand, TResponse>(services).Reverse())
        {
            var next = pipeline;
            pipeline = async () =>
            {
                try
                {
                    return await behavior.Handle(typedRequest, next, ct).ConfigureAwait(false);
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
