namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that the caller is not authorized to perform the requested operation.
/// </summary>
/// <param name="Reason">An optional description of why access was denied.</param>
public readonly record struct Forbidden(string? Reason = null);
