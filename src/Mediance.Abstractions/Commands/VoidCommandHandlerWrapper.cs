namespace Mediance.Abstractions.Commands;

using Mediance.Abstractions.Core;
using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;

/// <summary>Wraps a void command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The <see cref="IOperationOutcome"/>-shaped outcome type produced by the handler.</typeparam>
internal sealed class VoidCommandHandlerWrapper<TCommand, TResult> : VoidHandlerWrapper<TResult>
    where TCommand : ICommand
    where TResult : IOperationOutcome
{
    public override Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();

        Func<Task<TResult>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline();
    }
}
