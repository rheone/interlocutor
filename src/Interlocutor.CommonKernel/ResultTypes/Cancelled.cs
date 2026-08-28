namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Indicates that an operation was cooperatively cancelled mid-flight.
/// </summary>
/// <param name="Reason">An optional human-readable description of why the operation was cancelled.</param>
/// <remarks>
/// <para>
/// Use <see cref="Cancelled"/> when a handler observes a <see cref="System.Threading.CancellationToken"/>
/// requesting cancellation and stops the operation cooperatively, rather than letting an
/// <see cref="System.OperationCanceledException"/> propagate as an unexpected fault.
/// </para>
/// <para>
/// This case is reason-only, matching the shape of <see cref="NotFound"/> and <see cref="Forbidden"/>. This is
/// intentional, not an oversight relative to the richer shapes of <see cref="Timeout"/> or
/// <see cref="RateLimited"/>: a <see cref="System.Threading.CancellationToken"/> carries no queryable reason in
/// .NET, so there is no structured data to preserve beyond an optional caller-supplied description.
/// </para>
/// </remarks>
[Obsolete("May be overkill to include")]
public readonly record struct Cancelled(string? Reason = null)
{
    /// <summary>
    /// Returns a human-readable representation of this cancellation.
    /// </summary>
    /// <returns><c>"Cancelled"</c> when <see cref="Reason"/> is <see langword="null"/>; otherwise <c>"Cancelled: {Reason}"</c>.</returns>
    public override string ToString() => this.Reason is null ? "Cancelled" : $"Cancelled: {this.Reason}";
}
