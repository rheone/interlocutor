namespace Interlocutor.DependencyInjection.Microsoft;

using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Microsoft.Extensions.DependencyInjection implementation of <see cref="IRootScopeDetector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registered scoped, alongside the mediator itself, so it is reconstructed fresh - and its ambient
/// <see cref="IServiceProvider"/> re-captured - for every scope. Compares the ambient
/// <see cref="IServiceProvider"/> this instance was constructed with (the current resolving scope, or the
/// root provider itself when there is no scope) against <see cref="RootServiceProviderMarker.RootProvider"/>
/// (always the true root - see that type's remarks for why). Reference equality between the two means this
/// instance was resolved directly from the root provider, with no intervening
/// <c>IServiceScopeFactory.CreateScope()</c> call.
/// </para>
/// </remarks>
internal sealed class MicrosoftRootScopeDetector(IServiceProvider ambientProvider, RootServiceProviderMarker rootMarker)
    : IRootScopeDetector
{
    /// <inheritdoc/>
    public bool IsRootScope => ReferenceEquals(ambientProvider, rootMarker.RootProvider);
}
