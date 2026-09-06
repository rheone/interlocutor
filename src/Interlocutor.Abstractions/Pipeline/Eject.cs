namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Forces a pipeline dispatch to end immediately with a developer-chosen outcome, from anywhere inside a
/// handler's or pipeline behavior's own call graph.
/// </summary>
/// <remarks>
/// <para>
/// Use this when a handler or behavior discovers, possibly several method calls deep in its own logic, that
/// the operation has reached a dead end and there is nothing further to gain from continuing - rather than
/// manually threading a chosen <c>IOutcome</c> value back up through every intermediate call frame,
/// call <see cref="With{TResponse}(TResponse)"/> and let the pipeline's dispatch machinery recover it at the
/// nearest enclosing <c>Handle</c> call. See <see cref="PipelineEjectException{TResponse}"/>'s remarks for the
/// full unwind contract and why this is distinct from cooperative cancellation.
/// </para>
/// <para>
/// Ejecting carries no transactional meaning by itself: a unit-of-work commit gate (for example
/// <c>Interlocutor.Persistence.EntityFrameworkCore.UnitOfWorkBehavior</c> or its NHibernate counterpart)
/// commits by default regardless of whether the response it receives was ejected or returned normally, and
/// regardless of what case that response carries - an ejected <c>Conflict</c> commits exactly like a returned
/// one unless the ejecting code separately calls <see cref="NoCommit.AndEject{TResponse}(TResponse)"/> instead
/// of this method, which signals the opposite disposition alongside the same ejected value.
/// </para>
/// <para>
/// When <c>TResponse</c> is an unconstrained generic type parameter (typical when authoring a reusable
/// pipeline behavior), two options exist. Construct the chosen case value first via
/// <c>IOutcomeCaseRegistry.ConstructOrThrow</c>, then pass the result here - this method performs no case
/// construction of its own, it only converts an already-typed value into the throw. Or, PROTOTYPE ONLY (see
/// <see cref="WithCase{TCase}(TCase)"/>'s remarks): eject the case value itself via
/// <see cref="WithCase{TCase}(TCase)"/> and let the one enclosing catch site that already knows
/// <c>TResponse</c> concretely construct the response instead - the shape
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> is migrated to as this spike's worked example.
/// </para>
/// <code>
/// private Task&lt;Outcome&lt;Success, Conflict&gt;&gt; ApplyDiscount(Order order)
/// {
///     if (!TryReserveInventory(order))
///     {
///         // Several calls deep in a handler's own logic - return the chosen outcome directly rather than
///         // threading it back up through every intermediate frame.
///         return Task.FromResult(Eject.With&lt;Outcome&lt;Success, Conflict&gt;&gt;(new Conflict("Inventory already reserved")));
///     }
///
///     return Task.FromResult&lt;Outcome&lt;Success, Conflict&gt;&gt;(new Success());
/// }
/// </code>
/// </remarks>
public static class Eject
{
    /// <summary>
    /// Ejects the current pipeline dispatch, forcing it to end with <paramref name="response"/> instead of
    /// whatever the calling code would otherwise have produced.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type to return.</typeparam>
    /// <param name="response">The outcome the dispatch should return.</param>
    /// <returns>Never returns - always throws. Typed as <typeparamref name="TResponse"/> so it can be used directly in a <see langword="return"/> statement.</returns>
    /// <exception cref="PipelineEjectException{TResponse}">Always thrown - caught by the pipeline's dispatch machinery, never observed by callers of the mediator.</exception>
    public static TResponse With<TResponse>(TResponse response)
        where TResponse : IUnion => throw new PipelineEjectException<TResponse>(response);

    /// <summary>
    /// PROTOTYPE (spike/eject-withcase-case-fidelity): builds the exception that ejects the current pipeline
    /// dispatch with <paramref name="caseValue"/>, without the caller knowing - or needing to know - what the
    /// dispatch's concrete response type is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="With{TResponse}(TResponse)"/>, which requires an already-constructed
    /// <c>TResponse</c> and throws internally (so a call reads naturally in a <see langword="return"/>
    /// statement), this method is generic only in <typeparamref name="TCase"/> - the case value being
    /// ejected, not the response it will eventually become - and does not throw itself: it returns the
    /// exception to throw, so the call site reads <c>throw Eject.WithCase(new Forbidden(...));</c>, the same
    /// way any other exception is thrown. This is what lets a reusable pipeline behavior (generic over an
    /// unconstrained <c>TResponse : IUnion</c>, and so unable to construct one itself) eject a case with no
    /// <c>IOutcomeCaseRegistry</c> dependency of its own: construction is deferred entirely to the pipeline's
    /// dispatch machinery, which resolves the concrete <c>TResponse</c> the moment it catches the returned
    /// <see cref="PipelineEjectCaseException"/>. See that type's remarks for why it is not itself generic over
    /// <typeparamref name="TCase"/>, and why that is what makes a single shared catch site able to catch every
    /// case any behavior might eject.
    /// </para>
    /// <para>
    /// This is a spike prototype, not a shipped API: today only
    /// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> uses it, as the worked example sizing the
    /// blast radius of migrating every case-constructing behavior to this shape.
    /// </para>
    /// </remarks>
    /// <typeparam name="TCase">The case type being ejected.</typeparam>
    /// <param name="caseValue">The case value the dispatch's eventual response should be constructed from.</param>
    /// <returns>
    /// A <see cref="PipelineEjectCaseException"/> carrying <paramref name="caseValue"/> - the caller must
    /// throw it, this method only constructs it.
    /// </returns>
    public static PipelineEjectCaseException WithCase<TCase>(TCase caseValue)
        where TCase : notnull => new(caseValue);
}
