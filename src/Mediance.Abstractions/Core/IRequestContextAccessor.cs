namespace Mediance.Abstractions.Core;

/// <summary>Scoped ambient context carrying correlation ID, user identity, and tenant information through the request pipeline.</summary>
/// <remarks>Null-safe — if no middleware populates this, <see cref="Context"/> is null. Consumers must handle gracefully.</remarks>
public interface IRequestContextAccessor
{
    /// <summary>Gets or sets the current request context. May be null if not populated.</summary>
    /// <value>
    /// The current request context. May be null if not populated.
    /// </value>
    RequestContext? Context { get; set; }
}
