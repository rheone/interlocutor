namespace Interlocutor.Abstractions.Commands;

using System.Diagnostics;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;

/// <summary>Wraps a value-returning command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
/// <remarks>
/// <para>
/// Resolves the command handler and all applicable pipeline behaviors from DI, then composes them into a chain
/// in reverse registration order (so the first-registered behavior runs first inbound / last outbound). This wrapper
/// is created once per command type and cached by the mediator to avoid repeated reflection.
/// </para>
/// </remarks>
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : HandlerWrapper<TResponse>
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
    public override Task<TResponse> Handle(
        object request,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    )
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        activity?.SetTag(MediatorActivitySource.HandlerTypeTag, handler.GetType().FullName);

        // Start with the innermost pipeline delegate pointing to the handler itself.
        Func<Task<TResponse>> pipeline = () => handler.Handle(typedRequest, ct);

        // Wrap the handler with each behavior in reverse registration order so the first-registered behavior
        // runs first (inbound) and last (outbound), creating an onion-like structure. Each behavior wraps
        // the previous pipeline, capturing it in a closure.
        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline();
    }
}
