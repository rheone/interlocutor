namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Represents a successful outcome with no associated data.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Success"/> as the success case in operation outcome unions for void operations. It signals that
/// an operation completed successfully but produced no data to return (e.g., a delete command, or an operation that
/// only mutates state). When you need to return data on success, use a domain type instead of <see cref="Success"/>.
/// </para>
/// <para>
/// Example: <c>Task&lt;Outcome&lt;Success, NotFound, Failure&gt;&gt;</c> for a command that succeeds with no data,
/// or fails with not-found or generic failure.
/// </para>
/// </remarks>
public readonly record struct Success
{
    /// <summary>
    /// Returns a fixed, human-readable representation of this outcome.
    /// </summary>
    /// <returns>The literal string <c>"Success"</c>.</returns>
    public override string ToString() => "Success";
}
