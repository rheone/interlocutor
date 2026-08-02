namespace Mediance.Abstractions.Core;

/// <summary>Defines the base contract for handlers that produce a typed response.</summary>
/// <typeparam name="TResponse">The type of the success value produced by the handler.</typeparam>
internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct);
}
