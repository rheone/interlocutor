namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Configuration for the shipped percentage-rollout gate - the built-in predicate source for
/// <c>InterlocutorPipelineBuilder.AddConditionalBehavior</c>'s canary use case, letting a new or risky
/// behavior ramp from 0% to 100% of traffic via a config value rather than a redeploy.
/// </summary>
/// <remarks>
/// A plain POCO by design: the container-neutral core (this type) stays configuration-mechanism-agnostic.
/// Binding it to live configuration - <c>IOptionsMonitor&lt;RolloutOptions&gt;</c>, keyed by name so more
/// than one canary can run concurrently - is <c>Interlocutor.DependencyInjection.Microsoft</c>-specific
/// sugar (its <c>AddRolloutBehavior</c> extension method); <c>Interlocutor.DependencyInjection.Autofac</c>'s
/// identically-named extension works from a fixed, non-reloading snapshot of this type instead, since
/// Autofac has no <c>IOptionsMonitor</c> equivalent. See <c>documentation/pipeline-and-behaviors.md</c> for
/// a worked example and the capability difference between the two.
/// </remarks>
public sealed class RolloutOptions
{
    /// <summary>
    /// Gets or sets the percentage of dispatches the gated behavior should run for, from <c>0</c> (never)
    /// to <c>100</c> (always).
    /// </summary>
    /// <value>The percentage of dispatches the gated behavior should run for, from <c>0</c> to <c>100</c>.</value>
    public double PercentageEnabled { get; set; }
}
