namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Framework-agnostic ambient <see cref="IRequestContextAccessor"/> implementation with scoped lifetime.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides a thread-safe, scoped storage for <see cref="RequestContext"/> information
/// without dependency on ASP.NET Core HTTP context. It's suitable for any application type: web, worker service,
/// console application, or testing.
/// </para>
/// <para>
/// Register this with <c>AddScoped</c> lifetime so each request/operation gets its own context instance.
/// </para>
/// <para>
/// The context is null-safe: if not populated during request processing, accessing <see cref="Context"/> returns null.
/// Consumers must handle the null case gracefully.
/// </para>
/// </remarks>
public sealed class AmbientRequestContextAccessor : IRequestContextAccessor
{
    /// <inheritdoc/>
    public RequestContext? Context { get; set; }
}
