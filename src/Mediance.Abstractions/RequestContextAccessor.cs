namespace Mediance.Abstractions;

/// <summary>Default scoped implementation of <see cref="IRequestContextAccessor"/>. Stores context in an ambient property.</summary>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    /// <summary>Gets or sets the current request context. May be null if not populated.</summary>
    public RequestContext? Context { get; set; }
}
