namespace Interlocutor.Abstractions.Pipeline.Diagnostics;

/// <summary>
/// The result of <see cref="IPipelineExplainer.Explain"/> - the resolved pipeline behavior chain for one
/// request/response type pair, plus which Tier-1 override rule (if any) produced it.
/// </summary>
/// <param name="RequestType">The request type <see cref="IPipelineExplainer.Explain"/> was asked about.</param>
/// <param name="ResponseType">The response type <see cref="IPipelineExplainer.Explain"/> was asked about.</param>
/// <param name="ResolvedBehaviorTypes">
/// The resolved behaviors' concrete runtime types, in the same order
/// <c>PipelineChainResolver.ResolveChain</c> returned their instances - the general-purpose baseline's
/// registration order, or a matched Tier-1 override rule's declared order. Empty when nothing resolves (for
/// example, a request/response pair no registered behavior applies to).
/// </param>
/// <param name="MatchedOverrideRuleName">
/// The name of the Tier-1 <c>InterlocutorPipelineBuilder.OverridePipelineFor</c> rule that matched
/// <paramref name="RequestType"/>, or <see langword="null"/> when no override rule matched and
/// <paramref name="ResolvedBehaviorTypes"/> reflects the general-purpose baseline instead.
/// </param>
public sealed record PipelineExplanation(
    Type RequestType,
    Type ResponseType,
    IReadOnlyList<Type> ResolvedBehaviorTypes,
    string? MatchedOverrideRuleName
);
