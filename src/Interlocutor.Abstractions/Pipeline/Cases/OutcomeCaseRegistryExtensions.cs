namespace Interlocutor.Abstractions.Pipeline.Cases;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Convenience extensions over <see cref="IOutcomeCaseRegistry"/> for pipeline behaviors that short-circuit a
/// request with a specific outcome case (<c>Forbidden</c>, <c>Cancelled</c>, <c>Timeout</c>, <c>RateLimited</c>,
/// <c>ValidationErrors</c>, ...).
/// </summary>
public static class OutcomeCaseRegistryExtensions
{
    /// <summary>
    /// Constructs a <typeparamref name="TResponse"/> whose active case is <paramref name="caseValue"/>, or
    /// throws a behavior-specific <see cref="InvalidOperationException"/> if <typeparamref name="TResponse"/>
    /// has no <typeparamref name="TCase"/> case to construct.
    /// </summary>
    /// <remarks>
    /// Every pipeline behavior that short-circuits a request with a specific outcome case needs the same
    /// shape here: attempt <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}(TCase)"/>, and if
    /// <typeparamref name="TResponse"/> can't express the case, re-throw
    /// <see cref="OutcomeCaseNotSupportedException"/> - a registry-level configuration signal - as an
    /// <see cref="InvalidOperationException"/> naming the specific behavior and requirement, since the
    /// underlying registry has no way to know which behavior or marker interface is actually responsible for
    /// the mismatch. This extension centralizes that wrapping so each behavior only supplies the pieces of the
    /// message that are actually specific to it.
    /// </remarks>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type to construct.</typeparam>
    /// <typeparam name="TCase">The case type being constructed.</typeparam>
    /// <param name="caseRegistry">The registry to construct <typeparamref name="TResponse"/> from.</param>
    /// <param name="caseValue">The case value the constructed response should carry.</param>
    /// <param name="behaviorName">
    /// The calling behavior's closed generic name (e.g. <c>$"AuthorizationBehavior&lt;{typeof(TRequest).Name}, {typeof(TResponse).Name}&gt;"</c>),
    /// used to open the exception message.
    /// </param>
    /// <param name="eventPhrase">
    /// A short phrase describing what the behavior just did to warrant short-circuiting (e.g. <c>"denied a request"</c>).
    /// </param>
    /// <param name="requirementClause">
    /// A complete sentence (no trailing period) stating what response shape the caller must use instead (e.g.
    /// <c>$"A request implementing {nameof(IRequiresPolicy)} must use a response type with a {nameof(Forbidden)} case"</c>).
    /// </param>
    /// <returns>A <typeparamref name="TResponse"/> whose active case is <paramref name="caseValue"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TResponse"/> has no <typeparamref name="TCase"/> case to construct.
    /// </exception>
    public static TResponse ConstructOrThrow<TResponse, TCase>(
        this IOutcomeCaseRegistry caseRegistry,
        TCase caseValue,
        string behaviorName,
        string eventPhrase,
        string requirementClause
    )
        where TResponse : IUnion
    {
        try
        {
            return caseRegistry.Construct<TResponse, TCase>(caseValue);
        }
        catch (OutcomeCaseNotSupportedException ex)
        {
            throw new InvalidOperationException(
                $"{behaviorName} {eventPhrase}, but {typeof(TResponse).Name} has no {typeof(TCase).Name} case "
                    + $"to construct. {requirementClause}.",
                ex
            );
        }
    }
}
