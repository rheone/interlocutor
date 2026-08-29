namespace Interlocutor.Abstractions.Core;

using System.Diagnostics;

/// <summary>Defines the base contract for handlers that produce a typed response.</summary>
/// <typeparam name="TResponse">The type of the success value produced by the handler.</typeparam>
/// <remarks>
/// <para>
/// This is an internal abstraction used by the mediator to wrap both query and command handlers. Concrete
/// implementations (e.g., <c>QueryHandlerWrapper</c>, <c>CommandHandlerWrapper</c>) construct the pipeline behavior
/// chain at invocation time and delegate to the actual handler implementation.
/// </para>
/// <para>
/// Was an abstract class before Sonar's S1694 flagged it as interface-shaped (no fields, no non-abstract
/// members). Converting it was safe: every consumer only ever casts a freshly <see cref="Activator"/>-constructed
/// instance to this type (see <c>Mediator</c>'s query/command dispatch) and calls <see cref="Handle"/> - no
/// shared state, no base-class extension point, and no <c>tests/Interlocutor.Architecture.Tests</c> assumption
/// tied to it being a class. It stays <see langword="internal"/>, so this isn't a public API shape change.
/// </para>
/// </remarks>
internal interface IHandlerWrapper<TResponse>
{
    /// <summary>
    /// Executes the handler with the given request, resolving behaviors and the handler from the service provider.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="services">The service provider for resolving handlers and behaviors.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="activity">
    /// The <see cref="Activity"/> the mediator started for this dispatch, or <see langword="null"/> when nothing is
    /// listening. Concrete wrappers tag it with the resolved handler type once resolution completes; the null case
    /// must stay a no-op so the unlistened dispatch path performs no extra work.
    /// </param>
    /// <returns>The response from the handler.</returns>
    Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null);
}
