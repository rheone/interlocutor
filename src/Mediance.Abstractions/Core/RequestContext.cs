namespace Mediance.Abstractions.Core;

/// <summary>Immutable snapshot of request-scoped ambient data.</summary>
public sealed record RequestContext
{
    /// <summary>Gets a unique identifier for correlating this request across services. Auto-generated if not set.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the authenticated user identifier, or null if anonymous.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the tenant identifier for multi-tenant scenarios, or null if not scoped.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets an arbitrary key-value bag for extension data, or null if not set.</summary>
    public IReadOnlyDictionary<string, object>? Items { get; init; }
}
