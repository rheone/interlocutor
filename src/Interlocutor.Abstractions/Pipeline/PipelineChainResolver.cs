namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Resolves the ordered <see cref="IPipelineBehavior{TRequest,TResponse}"/> list for one dispatch - the
/// single decision point every dispatch path consults so a per-request-type pipeline override is enforced
/// identically and exactly once, regardless of which path a request travels.
/// </summary>
/// <remarks>
/// <para>
/// Both <see cref="PipelineChainExecutor"/> - shared by every hand-written handler wrapper
/// (<c>QueryHandlerWrapper</c>, <c>CommandHandlerWrapper</c>, <c>BareCommandHandlerWrapper</c>) - and the
/// source-generator's emitted dispatch wrappers call <see cref="ResolveChain{TRequest,TResponse}"/> instead
/// of resolving <see cref="IPipelineBehavior{TRequest,TResponse}"/> themselves, so this is the one place
/// either dispatch path's chain-building logic changes to add override support - not a per-behavior check
/// every behavior (including a consumer's own) would otherwise have to remember to perform. A request
/// matched by an override rule that also structurally satisfies an existing marker interface
/// (<c>IAuditableRequest</c>, <c>IRequiresPolicyRequest</c>, etc.) never runs that marker-constrained behavior
/// unless the override rule names it explicitly - the returned list is the override's declared set and
/// nothing else.
/// </para>
/// <para>
/// Callers fold the returned list in reverse, exactly as they always have, so its first entry - baseline
/// registration order, or an override's first declared behavior - remains the outermost, first-to-run
/// behavior.
/// </para>
/// </remarks>
public static class PipelineChainResolver
{
    /// <summary>
    /// Resolves the ordered behavior list for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type being dispatched.</typeparam>
    /// <typeparam name="TResponse">The handler's response type.</typeparam>
    /// <param name="services">The service provider to resolve behaviors, and the optional override registry, from.</param>
    /// <returns>
    /// The general-purpose baseline (<c>services.GetServices&lt;IPipelineBehavior&lt;TRequest,TResponse&gt;&gt;()</c>,
    /// in registration order) when no <see cref="IPipelineOverrideRegistry"/> is registered or none of its
    /// rules match <typeparamref name="TRequest"/>; otherwise exactly the matched rule's declared behaviors,
    /// in declared order - a behavior type the baseline did not resolve an instance for is silently omitted,
    /// since <c>InterlocutorPipelineBuilder.OverridePipelineFor</c> always registers every declared behavior
    /// type as an ordinary behavior itself, so this only happens for a container assembled by hand rather
    /// than through that builder.
    /// </returns>
    public static IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> ResolveChain<TRequest, TResponse>(
        IServiceProvider services
    )
    {
        var baseline = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

        var registry = (IPipelineOverrideRegistry?)services.GetService(typeof(IPipelineOverrideRegistry));
        var match = registry?.TryGetOverride(typeof(TRequest));
        if (match is null)
        {
            return baseline;
        }

        var ordered = new List<IPipelineBehavior<TRequest, TResponse>>(match.BehaviorTypes.Count);
        foreach (var declaredType in match.BehaviorTypes)
        {
            var resolvedInstance = baseline.Find(behavior => IsInstanceOf(behavior.GetType(), declaredType));
            if (resolvedInstance is not null)
            {
                ordered.Add(resolvedInstance);
            }
        }

        return ordered;
    }

    /// <summary>
    /// Tests whether <paramref name="instanceType"/> - a resolved behavior's runtime type - is the closed
    /// form of <paramref name="declaredType"/> when it is an open generic type definition, or an exact
    /// match when <paramref name="declaredType"/> is already closed.
    /// </summary>
    private static bool IsInstanceOf(Type instanceType, Type declaredType) =>
        declaredType.IsGenericTypeDefinition
            ? instanceType.IsGenericType && instanceType.GetGenericTypeDefinition() == declaredType
            : instanceType == declaredType;
}
