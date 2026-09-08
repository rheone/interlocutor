namespace Interlocutor.Abstractions.Pipeline.Diagnostics;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A diagnostic entry point that answers, against a live, already-built <see cref="IServiceProvider"/>,
/// which pipeline behaviors resolve for a given request/response type pair - the runtime counterpart to
/// <c>InterlocutorBuilder.DescribeRegistrations()</c>, which only reports the builder's own
/// pre-container-build configuration.
/// </summary>
/// <remarks>
/// <para>
/// Intended for a human who has a <see cref="Type"/> in hand - typically from a bug report or a
/// configuration file, not a compile-time generic argument - a support engineer, an admin endpoint, or a
/// test. <c>PipelineChainResolver.ResolveChain</c> already computes the real answer for a
/// compile-time-known request/response pair and every dispatch path already calls it; this is a friendlier,
/// reflective wrapper over that same decision for the runtime-typed case, not a second implementation of its
/// logic.
/// </para>
/// <para>
/// Opt-in and disabled by default (see <see cref="PipelineExplainerOptions.Enabled"/>): this resolves live
/// instances from the container and reveals internal wiring, which is not something to run unexamined
/// against a production container.
/// </para>
/// </remarks>
public interface IPipelineExplainer
{
    /// <summary>
    /// Resolves and describes the pipeline behavior chain <paramref name="requestType"/>/<paramref name="responseType"/>
    /// would actually resolve from <paramref name="services"/>.
    /// </summary>
    /// <param name="requestType">The request's runtime type - a closed type, not an open generic definition.</param>
    /// <param name="responseType">The handler's response type - a closed type, not an open generic definition.</param>
    /// <param name="services">The live, already-built service provider to resolve behaviors from.</param>
    /// <returns>The resolved, formatted explanation.</returns>
    /// <exception cref="InvalidOperationException"><see cref="PipelineExplainerOptions.Enabled"/> is <see langword="false"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="requestType"/> or <paramref name="responseType"/> is an open generic type definition.
    /// </exception>
    [RequiresDynamicCode(
        "Implementations close PipelineChainResolver.ResolveChain<,> reflectively via MakeGenericMethod for "
            + "the given requestType/responseType - the same JIT-closure requirement every dispatch path "
            + "already has for a request/response pair the runtime hasn't seen before. See "
            + "documentation/trim-and-aot-support.md."
    )]
    PipelineExplanation Explain(Type requestType, Type responseType, IServiceProvider services);
}
