namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// The random-draw evaluation behind the shipped percentage-rollout gate - shared, container-neutral logic
/// so <c>Interlocutor.DependencyInjection.Microsoft</c>'s and <c>Interlocutor.DependencyInjection.Autofac</c>'s
/// <c>AddRolloutBehavior</c> sugar (and any consumer composing <see cref="RolloutOptions"/> into their own
/// predicate directly, without either package's sugar) share one implementation, rather than each
/// re-deriving the boundary behavior at <see cref="RolloutOptions.PercentageEnabled"/> of <c>0</c> and
/// <c>100</c>.
/// </summary>
public static class RolloutGate
{
    /// <summary>
    /// Draws once against <paramref name="options"/>'s current <see cref="RolloutOptions.PercentageEnabled"/> -
    /// <see langword="false"/> always at <c>0</c>, <see langword="true"/> always at <c>100</c>, and
    /// approximately that fraction of calls at any value in between. There is no sticky-by-key assignment:
    /// two calls for the same logical caller can draw differently from one dispatch to the next - a
    /// fast-follow, not required for this first version.
    /// </summary>
    /// <param name="options">The rollout configuration to draw against.</param>
    /// <returns><see langword="true"/> if this draw falls within the configured percentage.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static bool ShouldRun(RolloutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Random.Shared.NextDouble() * 100.0 < options.PercentageEnabled;
    }
}
