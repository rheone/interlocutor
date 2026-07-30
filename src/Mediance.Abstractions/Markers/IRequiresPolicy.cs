namespace Mediance.Abstractions.Markers;

/// <summary>
/// Marks a request that must satisfy an authorization policy before execution.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline behaviors check <see cref="IRequiresPolicy"/> requests against the
/// configured authorization system using the <see cref="PolicyName"/> value
/// before delegating to the handler. If the policy is not satisfied, the
/// behavior short-circuits the pipeline with a forbidden result.
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
