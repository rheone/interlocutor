namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Represents a conflict error that occurred during command or query execution.
/// Analogous to an HTTP 409 Conflict response, indicating the request cannot be
/// completed due to a conflict with the current state of the target resource.
/// </summary>
/// <param name="code">A machine-readable error code identifying the conflict type.</param>
/// <param name="message">A human-readable description of the conflict.</param>
public sealed record Conflict(string message, string code);
