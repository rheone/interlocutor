namespace Mediance.CommonKernel;

/// <summary>
/// Indicates that a requested resource was not found.
/// </summary>
/// <param name="entityType">The type of entity that was not found.</param>
/// <param name="key">The identifier of the resource that was searched for.</param>
public sealed record NotFound(string entityType, object key);
