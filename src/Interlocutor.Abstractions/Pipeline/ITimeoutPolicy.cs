namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;

/// <summary>Resolves the effective time budget for a request implementing <see cref="ITimeoutRequest"/>.</summary>
/// <remarks>
/// <para>
/// Implement <see cref="ITimeoutPolicy"/> to override the budget a request declares for itself - for example,
/// clamping every request to a tenant-wide ceiling, or applying an environment-specific multiplier during
/// load testing. Register your implementation in DI, and <see cref="TimeoutBehavior{TRequest, TResponse}"/>
/// will invoke it to resolve the budget it races against.
/// </para>
/// <para>
/// This abstraction is deliberately minimal to avoid framework dependencies. <see cref="DefaultTimeoutPolicy"/>
/// simply returns the request's own <see cref="ITimeoutRequest.Budget"/> unchanged; <see cref="DelegateTimeoutPolicy"/>
/// lets a consumer supply inline override logic without implementing the interface directly.
/// </para>
/// </remarks>
public interface ITimeoutPolicy
{
    /// <summary>Resolves the effective time budget for <paramref name="request"/>.</summary>
    /// <param name="request">The request whose budget is being resolved.</param>
    /// <returns>The time budget <see cref="TimeoutBehavior{TRequest, TResponse}"/> should race against.</returns>
    TimeSpan ResolveBudget(ITimeoutRequest request);
}
