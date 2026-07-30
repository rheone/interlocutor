namespace Mediance.Abstractions.Pipeline;

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}
