namespace Mediance.Abstractions.Core;

using Mediance.CommonKernel;

/// <summary>Defines the base contract for handlers that produce an outcome-only (no data payload) result.</summary>
/// <typeparam name="TResult">The <see cref="IOperationOutcome"/>-shaped outcome type produced by the handler.</typeparam>
internal abstract class VoidHandlerWrapper<TResult>
    where TResult : IOperationOutcome
{
    public abstract Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct);
}
