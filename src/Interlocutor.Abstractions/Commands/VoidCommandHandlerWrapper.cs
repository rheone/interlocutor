namespace Interlocutor.Abstractions.Commands;

using System.Diagnostics;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Wraps a void command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The <see cref="IOutcome"/>-shaped outcome type produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// Resolves the void command handler from DI and tags the dispatch activity; <see cref="PipelineChainExecutor"/>
/// composes the pipeline behaviors around it (see that type's remarks for the chain-build/eject-guard logic
/// shared with <c>QueryHandlerWrapper</c> and <c>CommandHandlerWrapper</c>). This is the wrapper for commands
/// that return only outcomes (not domain data). The outcome type is resolved at call time based on the
/// caller's type argument, enabling flexible outcome handling without modifying the handler.
/// </para>
/// </remarks>
internal sealed class VoidCommandHandlerWrapper<TCommand, TResult> : IVoidHandlerWrapper<TResult>
    where TCommand : ICommand
    where TResult : IOutcome
{
    /// <summary>
    /// Executes the void command with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The outcome from the handler.</returns>
    public Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        return PipelineChainExecutor.ExecuteAsync(typedRequest, handler.Handle, services, ct);
    }
}
