namespace Interlocutor.Abstractions.Pipeline.Diagnostics;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// The default <see cref="IPipelineExplainer"/> implementation - resolves
/// <c>PipelineChainResolver.ResolveChain</c> reflectively via <see cref="MethodInfo.MakeGenericMethod"/> for
/// a request/response <see cref="Type"/> pair known only at run time, then projects each resolved behavior
/// instance to its runtime <see cref="Type"/>.
/// </summary>
/// <remarks>
/// This is an explicitly opt-in diagnostic path, not the hot dispatch path - see
/// <see cref="PipelineExplainerOptions.Enabled"/> and <see cref="Explain"/>'s own
/// <see cref="RequiresDynamicCodeAttribute"/> for the trim/AOT caveat this reflective call carries.
/// </remarks>
/// <param name="options">Gates whether <see cref="Explain"/> is permitted to run.</param>
public sealed class PipelineExplainer(PipelineExplainerOptions options) : IPipelineExplainer
{
    private static readonly MethodInfo ResolveChainMethod = ResolveResolveChainMethod();

    /// <inheritdoc/>
    [RequiresDynamicCode(
        "Closes PipelineChainResolver.ResolveChain<,> reflectively via MethodInfo.MakeGenericMethod for the "
            + "given requestType/responseType - the same JIT-closure requirement every dispatch path already "
            + "has for a request/response pair the runtime hasn't seen before. See "
            + "documentation/trim-and-aot-support.md."
    )]
    public PipelineExplanation Explain(Type requestType, Type responseType, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        ArgumentNullException.ThrowIfNull(services);

        if (!options.Enabled)
        {
            throw new InvalidOperationException(
                $"{nameof(IPipelineExplainer)} is disabled. Set {nameof(PipelineExplainerOptions)}.{nameof(PipelineExplainerOptions.Enabled)} "
                    + "to true - for example, builder.Pipeline.AddPipelineExplainer(new PipelineExplainerOptions "
                    + "{ Enabled = true }) - before calling Explain. This diagnostic path resolves live instances "
                    + "from the container and reveals internal wiring, so it is disabled by default rather than "
                    + "run unexamined against a production container."
            );
        }

        ThrowIfOpenGeneric(requestType, nameof(requestType));
        ThrowIfOpenGeneric(responseType, nameof(responseType));

        var closedResolveChain = ResolveChainMethod.MakeGenericMethod(requestType, responseType);
        var behaviors = (IEnumerable)closedResolveChain.Invoke(null, [services])!;
        var resolvedBehaviorTypes = behaviors.Cast<object>().Select(behavior => behavior.GetType()).ToList();

        var overrideRegistry = (IPipelineOverrideRegistry?)services.GetService(typeof(IPipelineOverrideRegistry));
        var match = overrideRegistry?.TryGetOverride(requestType);

        return new PipelineExplanation(requestType, responseType, resolvedBehaviorTypes, match?.RuleName);
    }

    /// <summary>
    /// Throws if <paramref name="type"/> is an open generic type definition - <see cref="Explain"/> needs the
    /// same closed types a dispatched request would actually resolve behaviors for, not an open generic
    /// definition <see cref="MethodInfo.MakeGenericMethod"/> cannot close over.
    /// </summary>
    private static void ThrowIfOpenGeneric(Type type, string paramName)
    {
        if (type.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"'{type}' is an open generic type definition. Explain needs a closed request/response type - "
                    + "the same kind of type a dispatched request would actually resolve behaviors for - not "
                    + "an open generic definition like 'MyRequest<>'.",
                paramName
            );
        }
    }

    /// <summary>
    /// Locates <c>PipelineChainResolver.ResolveChain</c> once, cached for the lifetime of the process - the
    /// <see cref="MethodInfo"/> <see cref="Explain"/> closes reflectively for each caller-supplied
    /// request/response <see cref="Type"/> pair.
    /// </summary>
    private static MethodInfo ResolveResolveChainMethod() =>
        typeof(PipelineChainResolver).GetMethod(nameof(PipelineChainResolver.ResolveChain))
        ?? throw new MissingMethodException(nameof(PipelineChainResolver), nameof(PipelineChainResolver.ResolveChain));
}
