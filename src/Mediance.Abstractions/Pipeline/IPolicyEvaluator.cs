namespace Mediance.Abstractions.Pipeline;

/// <summary>Minimal authorization abstraction that avoids coupling to ASP.NET Core's <c>IAuthorizationService</c>.</summary>
public interface IPolicyEvaluator
{
    /// <summary>Evaluates whether the request is authorized for the given policy.</summary>
    Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct);
}
