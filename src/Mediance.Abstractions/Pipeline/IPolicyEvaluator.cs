namespace Mediance.Abstractions.Pipeline;

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    /// <param name="policyName">The policy name to evaluate.</param>
    /// <param name="request">The request context for policy evaluation.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with a value indicating if the policy evaluated to authorized.</returns>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}
