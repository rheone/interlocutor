namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Default <see cref="IPolicyEvaluator"/> implementation suitable for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation always returns <see langword="true"/>, granting all policy evaluations. Suitable for:
/// <list type="bullet">
///   <item><description>Development environments where authorization is not yet implemented.</description></item>
///   <item><description>Testing scenarios where authorization should not block test execution.</description></item>
///   <item><description>Prototypes and proof-of-concepts.</description></item>
/// </list>
/// </para>
/// <para>
/// In production, replace this with a real authorization implementation before deploying.
/// </para>
/// </remarks>
public sealed class DefaultPolicyEvaluator : IPolicyEvaluator
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly DefaultPolicyEvaluator Instance = new();

    /// <inheritdoc/>
    public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct) => Task.FromResult(true);
}
