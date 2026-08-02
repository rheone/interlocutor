namespace Mediance.Abstractions.Commands;

using Mediance.Abstractions.Core;
using Mediance.Abstractions.Pipeline;

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
{
    public override Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        Func<Task<TResponse>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline();
    }
}
