namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that an operation exceeded its allotted time budget.
/// </summary>
/// <param name="Budget">The time budget that was allotted to the operation.</param>
/// <param name="Elapsed">The actual time elapsed before the operation was abandoned.</param>
/// <param name="Message">An optional human-readable description of the timeout.</param>
/// <remarks>
/// <para>
/// Use <see cref="Timeout"/> when a handler enforces a time budget (e.g., via a timeout pipeline behavior) and the
/// operation fails to complete within it. Unlike <see cref="Cancelled"/>, which carries no structured detail, a
/// timeout always knows both the declared <paramref name="Budget"/> and the actual <paramref name="Elapsed"/> time,
/// so both are preserved for diagnostics.
/// </para>
/// <para>
/// Provide a <paramref name="Message"/> for additional context, such as which downstream dependency was slow.
/// </para>
/// </remarks>
public readonly record struct Timeout(TimeSpan Budget, TimeSpan Elapsed, string? Message = null)
{
    /// <summary>
    /// Returns a human-readable representation of this timeout.
    /// </summary>
    /// <returns>
    /// <c>"Timeout: Budget={Budget}, Elapsed={Elapsed}"</c> when <see cref="Message"/> is <see langword="null"/>;
    /// otherwise <c>"Timeout: Budget={Budget}, Elapsed={Elapsed}, {Message}"</c>.
    /// </returns>
    public override string ToString() =>
        this.Message is null
            ? $"Timeout: Budget={this.Budget}, Elapsed={this.Elapsed}"
            : $"Timeout: Budget={this.Budget}, Elapsed={this.Elapsed}, {this.Message}";
}
