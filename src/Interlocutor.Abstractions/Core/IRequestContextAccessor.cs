namespace Interlocutor.Abstractions.Core;

/// <summary>Provides scoped ambient access to the correlation ID, user identity, and tenant information carried through the current request or operation.</summary>
/// <remarks>
/// <para>
/// Implementations are registered with a scoped lifetime so each request/operation gets its own
/// <see cref="RequestContext"/> instance. <see cref="AmbientRequestContextAccessor"/> is the framework-agnostic
/// implementation shipped here; a host framework may instead supply an implementation that populates
/// <see cref="Context"/> from its own ambient state (e.g. the current HTTP request).
/// </para>
/// <para>
/// Access is null-safe: if nothing has populated <see cref="Context"/> yet, reading it returns
/// <see langword="null"/> rather than throwing. Consumers must handle that case explicitly.
/// </para>
/// </remarks>
public interface IRequestContextAccessor
{
    /// <summary>Gets or sets the current request context.</summary>
    /// <value>The current request context, or <see langword="null"/> if nothing has populated it yet.</value>
    RequestContext? Context { get; set; }
}
