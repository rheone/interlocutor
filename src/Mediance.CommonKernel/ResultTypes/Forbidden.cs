namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that the caller is not authorized to perform the requested operation.
/// </summary>
/// <param name="Reason">An optional description of why access was denied.</param>
public sealed record Forbidden(string? Reason = null);
