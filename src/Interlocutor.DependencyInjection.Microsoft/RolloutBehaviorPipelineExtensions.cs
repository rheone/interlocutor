namespace Interlocutor.DependencyInjection.Microsoft;

using System.Diagnostics.CodeAnalysis;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Options;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Microsoft-DI-specific sugar over
/// <see cref="InterlocutorPipelineBuilder.AddConditionalBehavior{TBehavior,TRequest,TResponse}"/> for the
/// shipped percentage-rollout gate, binding <see cref="RolloutOptions"/> to a named
/// <see cref="IOptionsMonitor{TOptions}"/> registration so more than one canary can run concurrently and
/// <see cref="RolloutOptions.PercentageEnabled"/> can be adjusted live via configuration reload, without a
/// redeploy.
/// </summary>
/// <remarks>
/// See <c>documentation/pipeline-and-behaviors.md</c> for a worked example ramping a new validation rule
/// from 0% to 100%. <c>Interlocutor.DependencyInjection.Autofac</c>'s identically-named
/// <c>AddRolloutBehavior</c> overload has no live-reload capability - see its own remarks for why.
/// </remarks>
public static class RolloutBehaviorPipelineExtensions
{
    /// <summary>
    /// Adds <typeparamref name="TBehavior"/> to the chain gated by the shipped percentage-rollout gate,
    /// bound to the named <see cref="IOptionsMonitor{TOptions}"/> registration for <see cref="RolloutOptions"/> -
    /// typically set up with <c>services.Configure&lt;RolloutOptions&gt;(rolloutName, configuration.GetSection(...))</c>.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementation to gate.</typeparam>
    /// <typeparam name="TRequest">The request type the behavior participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the behavior participates in.</typeparam>
    /// <param name="pipeline">The pipeline builder to add the gated behavior to.</param>
    /// <param name="rolloutName">
    /// The named-options key identifying this canary's own <see cref="RolloutOptions"/> - the same name
    /// passed to <c>services.Configure&lt;RolloutOptions&gt;(rolloutName, ...)</c>, so more than one canary
    /// can be ramped independently and concurrently.
    /// </param>
    /// <param name="lifetime">The service lifetime to register the gate wrapper with. Defaults to scoped.</param>
    /// <returns><paramref name="pipeline"/>, for chaining.</returns>
    public static InterlocutorPipelineBuilder AddRolloutBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TBehavior,
        TRequest,
        TResponse
    >(
        this InterlocutorPipelineBuilder pipeline,
        string rolloutName,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentException.ThrowIfNullOrWhiteSpace(rolloutName);

        return pipeline.AddConditionalBehavior<TBehavior, TRequest, TResponse>(
            (services, _) =>
                RolloutGate.ShouldRun(services.GetRequiredService<IOptionsMonitor<RolloutOptions>>().Get(rolloutName)),
            lifetime
        );
    }
}
