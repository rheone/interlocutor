namespace Interlocutor.Abstractions.Core;

/// <summary>Immutable snapshot of request-scoped ambient data.</summary>
/// <remarks>
/// <para>
/// Immutable with respect to its own properties - none of them change after construction, so swap them via a
/// <see langword="with"/> expression, not a setter. <see cref="Items"/> is the one exception worth calling
/// out: the property itself is still <see langword="init"/>-only, but the <see cref="RequestContextItems"/>
/// object it refers to is a mutable bag - see that type's remarks.
/// </para>
/// </remarks>
public sealed record RequestContext
{
    /// <summary>Gets a unique identifier for correlating this request across services. Auto-generated if not set.</summary>
    /// <value>
    /// A unique identifier for correlating this request across services. Auto-generated if not set.
    /// </value>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the authenticated user identifier, or <see langword="null"/> if anonymous.</summary>
    /// <value>
    /// The authenticated user identifier, or <see langword="null"/> if anonymous.
    /// </value>
    public string? UserId { get; init; }

    /// <summary>Gets the tenant identifier for multi-tenant scenarios, or <see langword="null"/> if not scoped.</summary>
    /// <value>
    /// The tenant identifier for multi-tenant scenarios, or <see langword="null"/> if not scoped.
    /// </value>
    public string? TenantId { get; init; }

    /// <summary>Gets a mutable, thread-safe key-value bag for extension data. Never <see langword="null"/>.</summary>
    /// <value>
    /// A mutable, thread-safe key-value bag for extension data. Handlers and pipeline behaviors write into it
    /// in place; see <see cref="RequestContextItems"/>'s remarks for the full mutability and granularity
    /// contract.
    /// </value>
    public RequestContextItems Items { get; init; } = new();
}
