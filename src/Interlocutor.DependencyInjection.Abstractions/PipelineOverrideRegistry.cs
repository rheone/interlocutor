namespace Interlocutor.DependencyInjection.Abstractions;

using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// The container-neutral <see cref="IPipelineOverrideRegistry"/> implementation both
/// <c>Interlocutor.DependencyInjection.Microsoft</c> and <c>Interlocutor.DependencyInjection.Autofac</c>
/// register as a singleton from <see cref="InterlocutorPipelineBuilder.OverrideRules"/>, so
/// <c>PipelineChainResolver</c> resolves an identical answer regardless of which container built it.
/// </summary>
/// <remarks>
/// A conflict between two rules whose predicates both match the same request type is a
/// <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/> build-validation problem - but that
/// check is opt-in (<see cref="InterlocutorBuilder.ValidateOnBuild"/>). Absent that check, this type still
/// needs a deterministic answer: it returns the first matching rule in <see cref="InterlocutorPipelineBuilder.OverrideRules"/>'s
/// declared order, the same "first match wins, but a real conflict should be caught by validation instead of
/// relied upon" posture <see cref="TypeMatchRule"/>-driven registration already has.
/// </remarks>
/// <param name="rules">Every override rule declared via <see cref="InterlocutorPipelineBuilder.OverridePipelineFor"/>.</param>
public sealed class PipelineOverrideRegistry(IReadOnlyList<PipelineOverrideRule> rules) : IPipelineOverrideRegistry
{
    /// <inheritdoc/>
    public bool TryGetOverride(Type requestType, out IReadOnlyList<Type> behaviorTypes)
    {
        var match = rules.FirstOrDefault(rule => rule.Predicate(requestType));

        if (match is not null)
        {
            behaviorTypes = match.BehaviorTypes;
            return true;
        }

        behaviorTypes = [];
        return false;
    }
}
