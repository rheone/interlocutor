namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// The Tier-1 override rule that matched a request type, returned by
/// <see cref="IPipelineOverrideRegistry.TryGetOverride"/>.
/// </summary>
/// <remarks>
/// Carries both the declared behavior list and the rule's own <see cref="RuleName"/> as one value, rather
/// than the behavior list alone - a caller that also needs to identify which rule matched (for example,
/// <c>Interlocutor.Abstractions.Pipeline.Diagnostics.IPipelineExplainer</c>) reads it directly from the same
/// lookup instead of a second, parallel lookup into <c>InterlocutorPipelineBuilder.OverrideRules</c> to find
/// it by predicate re-evaluation.
/// </remarks>
/// <param name="BehaviorTypes">
/// The matched rule's complete, explicitly-ordered behavior list. Each entry is either an open generic type
/// definition (for example <c>typeof(MyBehavior&lt;,&gt;)</c>) or a closed implementation type - the same
/// shape <c>InterlocutorPipelineBuilder.AddBehavior</c> accepts.
/// </param>
/// <param name="RuleName">
/// The matched rule's own name, as declared via <c>InterlocutorPipelineBuilder.OverridePipelineFor</c> -
/// today also the identifier <c>RegistrationProblemKind.ConflictingPipelineOverride</c> messages use to name
/// which rules conflict.
/// </param>
public sealed record PipelineOverrideMatch(IReadOnlyList<Type> BehaviorTypes, string RuleName);
