namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that a requested resource was not found.
/// </summary>
/// <param name="Message">An optional human-readable description of what was not found.</param>
public readonly record struct NotFound(string? Message = null);

/// <summary>
/// Indicates that a resource of a specific type and identifier was not found.
/// </summary>
/// <typeparam name="TId">The type of the resource identifier.</typeparam>
/// <param name="EntityType">The type of entity that was not found.</param>
/// <param name="Id">The identifier of the resource that was searched for.</param>
public readonly record struct NotFound<TId>(Type EntityType, TId Id, string? Message = null);
