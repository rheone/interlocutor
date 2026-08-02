namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Represents a conflict error that occurred during command or query execution.
/// completed due to a conflict with the current state.
/// </summary>
/// <param name="code">A machine-readable error code identifying the conflict type.</param>
/// <param name="message">A human-readable description of the conflict.</param>
public readonly record struct Conflict(string message, string code);
