namespace Interlocutor.CommonKernel.ResultTypes;

[Obsolete("May be overkill to include")]
/// <summary>
/// Represents a conflict error that occurred during command or query execution.
/// </summary>
/// <param name="Message">A human-readable description of the conflict.</param>
/// <param name="Code">A machine-readable error code identifying the conflict type.</param>
/// <remarks>
/// <para>
/// Use <see cref="Conflict"/> when an operation cannot proceed because of a conflicting state in the domain.
/// Examples: attempting to delete an entity that's already deleted, or updating a record that's been modified
/// by another request (optimistic locking).
/// </para>
/// <para>
/// Provide both a <paramref name="Code"/> (for programmatic handling/retry logic) and a <paramref name="Message"/>
/// (for user-facing error descriptions).
/// </para>
/// </remarks>
public readonly record struct Conflict(string Message, string Code)
{
    /// <summary>
    /// Returns a human-readable representation of this conflict.
    /// </summary>
    /// <returns>
    /// <c>"Conflict: {Code} - {Message}"</c>. Unlike the other single-detail case types, both <see cref="Code"/> and
    /// <see cref="Message"/> are mandatory, so there is no bare form.
    /// </returns>
    public override string ToString() => $"Conflict: {this.Code} - {this.Message}";
}
