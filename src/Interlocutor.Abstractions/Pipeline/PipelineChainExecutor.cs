namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Builds and runs the pipeline chain around a resolved handler delegate.
/// </summary>
/// <remarks>
/// <para>
/// Extracted from the identical ~70-line chain-build/eject-guard logic every handler wrapper
/// (<c>QueryHandlerWrapper</c>, <c>CommandHandlerWrapper</c>, <c>BareCommandHandlerWrapper</c>) used to carry
/// its own copy of. Each wrapper still resolves its own handler type from DI and tags the dispatch
/// <see cref="System.Diagnostics.Activity"/> itself - only the part identical across all three (composing
/// <see cref="PipelineChainResolver.ResolveChain{TRequest,TResponse}"/>'s behaviors around the handler, with a
/// <see cref="PipelineEjectException{TResponse}"/> guard at every layer) lives here now. A prefactor: no
/// observable change to dispatch, pipeline ordering, or ejection.
/// </para>
/// </remarks>
internal static class PipelineChainExecutor
{
    /// <summary>
    /// Composes <paramref name="handle"/> with every behavior
    /// <see cref="PipelineChainResolver.ResolveChain{TRequest,TResponse}"/> resolves for
    /// <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>, and runs the resulting chain.
    /// </summary>
    /// <typeparam name="TRequest">The request type being dispatched.</typeparam>
    /// <typeparam name="TResponse">The <see cref="IUnion"/>-shaped response type the handler and every behavior produce.</typeparam>
    /// <param name="request">The request to pass to the handler and every behavior.</param>
    /// <param name="handle">The resolved handler's own <c>HandleAsync</c> method, already bound to its instance.</param>
    /// <param name="services">The service provider to resolve the behavior chain from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The response from running the composed chain.</returns>
    public static Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> handle,
        IServiceProvider services,
        CancellationToken ct
    )
        where TResponse : IUnion
    {
        // Start with the innermost pipeline delegate pointing to the handler itself. Wrapped in a try/catch
        // for PipelineEjectException so a handler that calls Eject.With deep in its own call graph returns an
        // ordinary response here - nothing further out in the chain ever observes a thrown exception.
        Func<Task<TResponse>> pipeline = async () =>
        {
            try
            {
                return await handle(request, ct).ConfigureAwait(false);
            }
            catch (PipelineEjectException<TResponse> eject)
            {
                return eject.Response;
            }
        };

        // Wrap the handler with each behavior in reverse order (baseline registration order, or a matched
        // override's declared order - see PipelineChainResolver) so the first entry runs first (inbound) and
        // last (outbound), creating an onion-like structure. Each behavior wraps the previous pipeline,
        // capturing it in a closure. Each wrap gets the same PipelineEjectException catch, so a behavior that
        // ejects - before or after calling next() - unwinds no further than its own caller in this chain.
        foreach (var behavior in PipelineChainResolver.ResolveChain<TRequest, TResponse>(services).Reverse())
        {
            var next = pipeline;
            pipeline = async () =>
            {
                try
                {
                    return await behavior.HandleAsync(request, next, ct).ConfigureAwait(false);
                }
                catch (PipelineEjectException<TResponse> eject)
                {
                    return eject.Response;
                }
            };
        }

        return pipeline();
    }
}
