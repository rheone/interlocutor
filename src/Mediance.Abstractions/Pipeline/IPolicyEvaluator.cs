namespace Mediance.Abstractions.Pipeline;

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
/// <remarks>
/// <para>
/// Implement <see cref="IPolicyEvaluator"/> to provide authorization decisions for requests implementing
/// <see cref="IRequiresPolicy"/>. Register your implementation in DI, and <see cref="AuthorizationBehavior{TRequest, TSuccess}"/>
/// will invoke it to check policies.
/// </para>
/// <para>
/// This abstraction is deliberately minimal to avoid framework dependencies. Your implementation can delegate to
/// ASP.NET Core Authorization, a custom rules engine, or any authorization system.
/// </para>
/// </remarks>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    /// <param name="policyName">The policy name to evaluate.</param>
    /// <param name="request">The request context for policy evaluation.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with a value indicating if the policy evaluated to authorized.</returns>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}
