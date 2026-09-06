namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request that declares a time budget it must complete within.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries that should be subject to a time budget. The
/// <see cref="Pipeline.TimeoutBehavior{TRequest, TResponse}"/> pipeline behavior intercepts these requests,
/// resolves the effective budget via <see cref="Pipeline.ITimeoutPolicy"/>, and races the rest of the pipeline
/// against it - short-circuiting with a <c>Timeout</c> case if the budget elapses first. This keeps
/// time-budget enforcement out of handlers and enables consistent timeout behavior across your application.
/// </para>
/// <para>
/// <see cref="Budget"/> is the request's own declared budget. <see cref="Pipeline.ITimeoutPolicy"/> can
/// override it (for example, to apply a tenant-wide ceiling), so the value <see cref="Pipeline.TimeoutBehavior{TRequest, TResponse}"/>
/// actually races against is whatever <see cref="Pipeline.ITimeoutPolicy.ResolveBudget(ITimeoutRequest)"/> returns,
/// not necessarily this property directly.
/// </para>
/// </remarks>
public interface ITimeoutRequest
{
    /// <summary>
    /// Gets the time budget this request should complete within.
    /// </summary>
    /// <value>
    /// The maximum duration allotted to the request before it is reported as timed out. Subject to
    /// override by the registered <see cref="Pipeline.ITimeoutPolicy"/>.
    /// </value>
    TimeSpan Budget { get; }
}
