namespace Mediance.Abstractions.Commands;

using Mediance.Abstractions.Core;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

/// <summary>Wraps a void command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The <see cref="IOperationOutcome"/>-shaped outcome type produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// Resolves the void command handler and all applicable pipeline behaviors from DI, then composes them into a chain.
/// This is the wrapper for commands that return only outcomes (not domain data). The outcome type is resolved at call
/// time based on the caller's type argument, enabling flexible outcome handling without modifying the handler.
/// </para>
/// </remarks>
internal sealed class VoidCommandHandlerWrapper<TCommand, TResult> : VoidHandlerWrapper<TResult>
    where TCommand : ICommand
    where TResult : IOperationOutcome
{
    public override Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();

        // Start with the innermost pipeline delegate pointing to the handler itself.
        Func<Task<TResult>> pipeline = () => handler.Handle(typedRequest, ct);

        // Wrap the handler with each behavior in reverse registration order so the first-registered behavior
        // runs first (inbound) and last (outbound), creating an onion-like structure. Each behavior wraps
        // the previous pipeline, capturing it in a closure.
        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline();
    }
}
