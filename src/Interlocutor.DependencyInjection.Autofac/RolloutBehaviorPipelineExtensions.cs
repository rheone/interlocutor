namespace Interlocutor.DependencyInjection.Autofac;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Autofac-specific sugar over
/// <see cref="InterlocutorPipelineBuilder.AddConditionalBehavior{TBehavior,TRequest,TResponse}"/> for the
/// shipped percentage-rollout gate, using a fixed <see cref="RolloutOptions.PercentageEnabled"/> value copied
/// at configuration time - not a reference to the caller's <see cref="RolloutOptions"/> instance, so
/// mutating that instance afterward has no effect.
/// </summary>
/// <remarks>
/// Autofac has no <c>IOptionsMonitor</c>-equivalent live-reload mechanism, so this package cannot offer the
/// same "adjustable via configuration reload, without a redeploy" capability
/// <c>Interlocutor.DependencyInjection.Microsoft</c>'s identically-named <c>AddRolloutBehavior</c> overload
/// does - this is a genuine capability difference, not a silent degrade. Changing the rollout percentage
/// under Autofac means rebuilding the container with a new <see cref="RolloutOptions"/> instance. See
/// <c>documentation/pipeline-and-behaviors.md</c> for a worked example.
/// </remarks>
public static class RolloutBehaviorPipelineExtensions
{
    /// <summary>
    /// Adds <typeparamref name="TBehavior"/> to the chain gated by the shipped percentage-rollout gate,
    /// evaluated against the fixed <paramref name="options"/> snapshot for the lifetime of the container.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementation to gate.</typeparam>
    /// <typeparam name="TRequest">The request type the behavior participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the behavior participates in.</typeparam>
    /// <param name="pipeline">The pipeline builder to add the gated behavior to.</param>
    /// <param name="options">
    /// The rollout configuration to snapshot at configuration time. Its
    /// <see cref="RolloutOptions.PercentageEnabled"/> value is copied immediately - not a reference to
    /// <paramref name="options"/> itself - so mutating this same instance afterward has no effect on an
    /// already-configured gate. Adjust the rollout percentage by rebuilding the container with a new value.
    /// </param>
    /// <param name="lifetime">The service lifetime to register the gate wrapper with. Defaults to scoped.</param>
    /// <returns><paramref name="pipeline"/>, for chaining.</returns>
    public static InterlocutorPipelineBuilder AddRolloutBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TBehavior,
        TRequest,
        TResponse
    >(
        this InterlocutorPipelineBuilder pipeline,
        RolloutOptions options,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(options);

        // Copied into a value the caller holds no reference to, rather than closing over `options` itself -
        // a genuine point-in-time snapshot, not a live view onto an instance the caller could keep mutating.
        var snapshot = new RolloutOptions { PercentageEnabled = options.PercentageEnabled };

        return pipeline.AddConditionalBehavior<TBehavior, TRequest, TResponse>(
            (_, _) => RolloutGate.ShouldRun(snapshot),
            lifetime
        );
    }
}
