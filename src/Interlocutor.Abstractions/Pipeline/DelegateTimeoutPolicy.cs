namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Markers;

/// <summary>
/// <see cref="ITimeoutPolicy"/> implementation that delegates budget resolution to a user-provided function.
/// </summary>
/// <remarks>
/// Use this implementation to provide custom budget-override logic without implementing the interface
/// directly - for example, registering <c>new DelegateTimeoutPolicy(r =&gt; r.Budget * 2)</c> in DI to double
/// every declared budget during a load test, or to clamp it to a tenant-wide ceiling. Useful for testing,
/// prototyping, or one-off overrides that don't warrant a named type.
/// </remarks>
/// <param name="resolveBudget">The delegate invoked from <see cref="ResolveBudget"/> to resolve the effective budget.</param>
/// <exception cref="ArgumentNullException"><paramref name="resolveBudget"/> is <see langword="null"/>.</exception>
public sealed class DelegateTimeoutPolicy(Func<ITimeoutRequest, TimeSpan> resolveBudget) : ITimeoutPolicy
{
    private readonly Func<ITimeoutRequest, TimeSpan> _resolveBudget =
        resolveBudget ?? throw new ArgumentNullException(nameof(resolveBudget));

    /// <inheritdoc/>
    public TimeSpan ResolveBudget(ITimeoutRequest request) => this._resolveBudget(request);
}
