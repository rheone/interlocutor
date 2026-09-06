namespace Interlocutor.Abstractions.Core;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Defines the base contract for handlers that produce an outcome-only (no data payload) result.</summary>
/// <typeparam name="TResult">The <see cref="IUnion"/>-shaped outcome type produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// This is an internal abstraction used by the mediator to wrap bare command handlers. Concrete implementations
/// (e.g., <c>BareCommandHandlerWrapper</c>) construct the pipeline behavior chain at invocation time.
/// </para>
/// <para>
/// Was an abstract class before Sonar's S1694 flagged it as interface-shaped (no fields, no non-abstract
/// members) - see <see cref="IHandlerWrapper{TResponse}"/>'s remarks for why converting its sibling was safe;
/// the same reasoning applies here.
/// </para>
/// </remarks>
internal interface IBareHandlerWrapper<TResult>
    where TResult : IUnion
{
    /// <summary>
    /// Executes the handler with the given request, resolving behaviors and the handler from the service
    /// provider.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="services">The service provider for resolving handlers and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">
    /// The <see cref="Activity"/> the mediator started for this dispatch, or <see langword="null"/> when nothing is
    /// listening. Concrete wrappers tag it with the resolved handler type once resolution completes; the null case
    /// must stay a no-op so the unlistened dispatch path performs no extra work.
    /// </param>
    /// <returns>The outcome from the handler.</returns>
    Task<TResult> HandleAsync(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null);
}
