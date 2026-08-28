namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Resolves which explicitly-declared, per-request-type override pipeline - if any - replaces the
/// general-purpose baseline for a given request type, at the point a dispatch's behavior chain is built.
/// </summary>
/// <remarks>
/// <para>
/// A registered implementation is built once, at container-build time, from every override rule a consumer
/// declared via <c>InterlocutorPipelineBuilder.OverridePipelineFor</c> - see that method for the two-tier
/// model (an explicit rule beats the general-purpose baseline entirely; an unmatched request draws from the
/// baseline exactly as it always has). <see cref="PipelineChainResolver"/> is the sole consumer of this
/// interface: both the hand-written handler wrappers and the source-generator's emitted dispatch wrappers
/// resolve it from the container and consult it before folding a request's behavior chain, so the override
/// guarantee is enforced once, centrally, regardless of which dispatch path a request travels.
/// </para>
/// <para>
/// Resolved as an optional service (<see cref="IServiceProvider.GetService(Type)"/>, not
/// <c>GetRequiredService</c>): a container built without ever calling <c>AddInterlocutor</c> - most test
/// setups that construct a bare <see cref="IServiceProvider"/> by hand - has none registered, which
/// <see cref="PipelineChainResolver"/> treats identically to a registered implementation whose rules simply
/// never match: the general-purpose baseline, unaffected.
/// </para>
/// </remarks>
public interface IPipelineOverrideRegistry
{
    /// <summary>
    /// Attempts to find the override rule that applies to <paramref name="requestType"/>.
    /// </summary>
    /// <param name="requestType">The request's runtime type.</param>
    /// <param name="behaviorTypes">
    /// When this method returns <see langword="true"/>, the matched rule's declared behavior types, in
    /// declared order. Each entry is either an open generic type definition (for example
    /// <c>typeof(MyBehavior&lt;,&gt;)</c>) or a closed implementation type - the same shape
    /// <c>InterlocutorPipelineBuilder.AddBehavior</c> accepts.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if exactly one override rule matches <paramref name="requestType"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool TryGetOverride(Type requestType, out IReadOnlyList<Type> behaviorTypes);
}
