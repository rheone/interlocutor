namespace Interlocutor.DependencyInjection.Autofac;

using global::Autofac;
using global::Autofac.Core.Lifetime;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Autofac implementation of <see cref="IRootScopeDetector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registered <c>InstancePerLifetimeScope</c>, alongside the mediator itself, so it is reconstructed fresh
/// for every lifetime scope. Autofac auto-injects <see cref="ILifetimeScope"/> as the ambient
/// currently-resolving scope without any explicit registration; the root <see cref="IContainer"/> built by
/// <c>ContainerBuilder.Build()</c> is itself an <see cref="ILifetimeScope"/> whose <see cref="ILifetimeScope.Tag"/>
/// is <see cref="LifetimeScope.RootTag"/>. A scope created via <c>BeginLifetimeScope()</c> (directly under the
/// root or nested further) carries a different tag, so comparing the ambient scope's tag against
/// <see cref="LifetimeScope.RootTag"/> identifies root-level resolution regardless of how many child scopes
/// have been created elsewhere in the application.
/// </para>
/// </remarks>
internal sealed class AutofacRootScopeDetector(ILifetimeScope ambientScope) : IRootScopeDetector
{
    /// <inheritdoc/>
    public bool IsRootScope => Equals(ambientScope.Tag, LifetimeScope.RootTag);
}
