namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Container-neutral abstraction the root-scope guard (see ADR-0011 and
/// <see cref="RootScopeGuardedMediator"/>) consults to know whether the ambient scope it was resolved into
/// is the root service provider/container itself, rather than a real per-request (or otherwise explicitly
/// created) child scope.
/// </summary>
/// <remarks>
/// <para>
/// Each container package (<c>Interlocutor.DependencyInjection.Microsoft</c>,
/// <c>Interlocutor.DependencyInjection.Autofac</c>) implements this interface using the mechanism its own
/// container exposes for distinguishing the root from a created scope - there is no container-neutral way to
/// answer this question, only a container-neutral place to ask it. Registered scoped by both container
/// packages alongside the mediator itself, so a fresh answer is computed for whichever scope (or lack of one)
/// the current resolution is happening in.
/// </para>
/// </remarks>
public interface IRootScopeDetector
{
    /// <summary>
    /// Gets a value indicating whether the ambient scope this detector was resolved into is the root service
    /// provider/container - i.e., no <c>CreateScope()</c>/<c>BeginLifetimeScope()</c> call sits between the
    /// application's root and this resolution.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when resolved directly from the root; <see langword="false"/> when resolved
    /// from within a real child scope.
    /// </value>
    bool IsRootScope { get; }
}
