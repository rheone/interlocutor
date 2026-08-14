namespace Interlocutor.Abstractions.Core;

/// <summary>Immutable snapshot of request-scoped ambient data.</summary>
public sealed record RequestContext
{
    /// <summary>Gets a unique identifier for correlating this request across services. Auto-generated if not set.</summary>
    /// <value>
    /// A unique identifier for correlating this request across services. Auto-generated if not set.
    /// </value>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the authenticated user identifier, or null if anonymous.</summary>
    /// <value>
    /// The authenticated user identifier, or null if anonymous.
    /// </value>
    public string? UserId { get; init; }

    /// <summary>Gets the tenant identifier for multi-tenant scenarios, or null if not scoped.</summary>
    /// <value>
    /// The tenant identifier for multi-tenant scenarios, or null if not scoped.
    /// </value>
    public string? TenantId { get; init; }

    /// <summary>Gets an arbitrary key-value bag for extension data, or null if not set.</summary>
    /// <value>
    /// An arbitrary key-value bag for extension data, or null if not set.
    /// </value>
    public IReadOnlyDictionary<string, object>? Items { get; init; }
}
