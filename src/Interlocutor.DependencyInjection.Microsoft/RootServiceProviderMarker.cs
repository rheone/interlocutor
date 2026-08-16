namespace Interlocutor.DependencyInjection.Microsoft;

/// <summary>
/// Singleton service whose sole purpose is capturing the root <see cref="IServiceProvider"/> via constructor
/// injection, for <see cref="MicrosoftRootScopeDetector"/> to compare against.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft.Extensions.DependencyInjection resolves a singleton service's constructor dependencies against
/// its own internally-tracked root scope regardless of which scope's resolution first triggered the
/// singleton's construction - so an <see cref="IServiceProvider"/> injected here is always the true root,
/// even if this marker happens to be constructed lazily from within a request scope rather than at startup.
/// This is what makes it a safe, stable point of comparison: a service resolved directly from the root
/// provider (the misuse case, see ADR-0011) receives that exact same <see cref="IServiceProvider"/> instance
/// via its own constructor injection, while a service resolved from within a real
/// <c>IServiceScopeFactory.CreateScope()</c> scope receives a different, scope-specific instance.
/// </para>
/// </remarks>
internal sealed class RootServiceProviderMarker(IServiceProvider rootProvider)
{
    /// <summary>Gets the root <see cref="IServiceProvider"/> captured at this singleton's construction.</summary>
    /// <value>The root <see cref="IServiceProvider"/> captured at this singleton's construction.</value>
    public IServiceProvider RootProvider { get; } = rootProvider;
}
