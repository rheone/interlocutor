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
/// Resolves the command handler from DI and tags the dispatch activity; <see cref="PipelineChainExecutor"/>
/// composes the pipeline behaviors around it (see that type's remarks for the chain-build/eject-guard logic
/// shared with <c>QueryHandlerWrapper</c> and <c>VoidCommandHandlerWrapper</c>). This wrapper is created once
/// per command type and cached by the mediator to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : IHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IOutcome
{
    /// <summary>
    /// Executes the command with its handler and pipeline behaviors.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="services">The service provider for resolving the handler and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">The activity to tag with the resolved handler type, or <see langword="null"/>.</param>
    /// <returns>The response from the handler.</returns>
    public Task<TResponse> HandleAsync(
        object request,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    )
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        return PipelineChainExecutor.ExecuteAsync(typedRequest, handler.HandleAsync, services, ct);
    }
}
