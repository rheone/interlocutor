namespace Interlocutor.Abstractions.Commands;

using System.Diagnostics;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Wraps a void command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The <see cref="IOperationOutcome"/>-shaped outcome type produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// Resolves the void command handler from DI, resolves the applicable pipeline behavior list via
/// <see cref="PipelineChainResolver"/> - the general-purpose baseline, or a matched per-request-type
/// override's declared list - then composes them into a chain. This is the wrapper for commands that return
/// only outcomes (not domain data). The outcome type is resolved at call time based on the caller's type
/// argument, enabling flexible outcome handling without modifying the handler.
/// </para>
/// </remarks>
internal sealed class VoidCommandHandlerWrapper<TCommand, TResult> : IVoidHandlerWrapper<TResult>
    where TCommand : ICommand
    where TResult : IOperationOutcome
{
    /// <summary>
    /// Executes the void command with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The outcome from the handler.</returns>
    public Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        // Start with the innermost pipeline delegate pointing to the handler itself. Wrapped in a try/catch
        // for PipelineEjectException so a handler that calls Eject.With deep in its own call graph returns an
        // ordinary response here - nothing further out in the chain ever observes a thrown exception.
        Func<Task<TResult>> pipeline = async () =>
        {
            try
            {
                return await handler.Handle(typedRequest, ct).ConfigureAwait(false);
            }
            catch (PipelineEjectException<TResult> eject)
            {
                return eject.Response;
            }
        };

        // Wrap the handler with each behavior in reverse order (baseline registration order, or a matched
        // override's declared order - see PipelineChainResolver) so the first entry runs first (inbound) and
        // last (outbound), creating an onion-like structure. Each behavior wraps the previous pipeline,
        // capturing it in a closure. Each wrap gets the same PipelineEjectException catch, so a behavior that
        // ejects - before or after calling next() - unwinds no further than its own caller in this chain.
        foreach (var behavior in PipelineChainResolver.ResolveChain<TCommand, TResult>(services).Reverse())
        {
            var next = pipeline;
            pipeline = async () =>
            {
                try
                {
                    return await behavior.Handle(typedRequest, next, ct).ConfigureAwait(false);
                }
                catch (PipelineEjectException<TResult> eject)
                {
                    return eject.Response;
                }
            };
        }

        return pipeline();
    }
}
