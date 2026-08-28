namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// A Tier-1 pipeline override rule added via <see cref="InterlocutorPipelineBuilder.OverridePipelineFor"/> -
/// a request type matched by <see cref="Predicate"/> gets exactly <see cref="BehaviorTypes"/>, in declared
/// order, in place of the general-purpose baseline (Tier 2) entirely.
/// </summary>
/// <remarks>
/// This is the reverse cardinality of <see cref="TypeMatchRule"/> in one respect and the same in another:
/// like <see cref="TypeMatchRule"/>, <see cref="Predicate"/> is tested against many request types and every
/// match is affected - but where a matched <see cref="TypeMatchRule"/> adds a registration, a matched
/// <see cref="PipelineOverrideRule"/> replaces a request's entire resolved pipeline. Two rules whose
/// predicates both match the same request type are a build-validation error (see
/// <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/>) - there is deliberately no
/// precedence-declaration mechanism to resolve the conflict silently.
/// </remarks>
/// <param name="Predicate">Tested against a request's runtime type at dispatch time.</param>
/// <param name="Name">
/// A label identifying this rule, required (unlike <see cref="TypeMatchRule.Name"/>) since it is the only
/// way a <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/> message can distinguish which
/// rules conflict.
/// </param>
/// <param name="BehaviorTypes">
/// The complete, explicitly-ordered behavior list a matched request resolves instead of the general-purpose
/// baseline. Each entry is either an open generic type definition or a closed implementation type - the
/// same shape <see cref="InterlocutorPipelineBuilder.AddBehavior"/> accepts.
/// </param>
public sealed record PipelineOverrideRule(Func<Type, bool> Predicate, string Name, IReadOnlyList<Type> BehaviorTypes);
