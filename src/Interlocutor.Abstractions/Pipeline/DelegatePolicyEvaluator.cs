namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// <see cref="IPolicyEvaluator"/> implementation that delegates policy evaluation to a user-provided function.
/// </summary>
/// <remarks>
/// <para>
/// Use this implementation to provide custom authorization logic without implementing the interface directly.
/// Useful for testing, prototyping, or integrating with custom authorization systems.
/// </para>
/// </remarks>
public sealed class DelegatePolicyEvaluator(Func<string, object, CancellationToken, Task<bool>> evaluateAsync)
    : IPolicyEvaluator
{
    private readonly Func<string, object, CancellationToken, Task<bool>> _evaluateAsync =
        evaluateAsync ?? throw new ArgumentNullException(nameof(evaluateAsync));

    /// <inheritdoc/>
    public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct) =>
        this._evaluateAsync(policyName, request, ct);
}
