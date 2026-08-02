namespace Mediance.Abstractions.Markers;

/// <summary>
/// Marks a request that must satisfy an authorization policy before execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries when access should be gated by an authorization policy.
/// The <see cref="AuthorizationBehavior{TRequest, TSuccess}"/> pipeline behavior intercepts these requests,
/// evaluates the policy via <see cref="IPolicyEvaluator"/>, and short-circuits with <see cref="Forbidden"/> if
/// the policy fails. This keeps authorization logic out of handlers and enables consistent policy enforcement
/// across your application.
/// </para>
/// <para>
/// The <see cref="PolicyName"/> is a string key that your authorization infrastructure (e.g., ASP.NET Core
/// Authorization) recognizes. Example: <c>"CanEditUsers"</c>, <c>"IsAdmin"</c>.
/// </para>
/// </remarks>
public interface IRequiresPolicy
{
    /// <summary>
    /// Gets the name of the authorization policy to enforce.
    /// </summary>
    /// <value>
    /// The policy identifier recognized by the authorization infrastructure,
    /// typically registered via a policy-based authorization scheme.
    /// </value>
    string PolicyName { get; }
}
