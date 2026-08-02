namespace Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Represents a conflict error that occurred during command or query execution.
/// </summary>
/// <param name="message">A human-readable description of the conflict.</param>
/// <param name="code">A machine-readable error code identifying the conflict type.</param>
/// <remarks>
/// <para>
/// Use <see cref="Conflict"/> when an operation cannot proceed because of a conflicting state in the domain.
/// Examples: attempting to delete an entity that's already deleted, or updating a record that's been modified
/// by another request (optimistic locking). In HTTP, this maps to a 409 status code.
/// </para>
/// <para>
/// Provide both a <paramref name="code"/> (for programmatic handling/retry logic) and a <paramref name="message"/>
/// (for user-facing error descriptions).
/// </para>
/// </remarks>
public readonly record struct Conflict(string message, string code);
