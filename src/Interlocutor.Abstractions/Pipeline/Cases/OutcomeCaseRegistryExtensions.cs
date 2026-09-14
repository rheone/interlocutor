namespace Interlocutor.Abstractions.Pipeline.Cases;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Convenience extensions over <see cref="IOutcomeCaseRegistry"/> for pipeline behaviors that short-circuit a
/// request with a specific outcome case (<c>Forbidden</c>, <c>Cancelled</c>, <c>Timeout</c>, <c>RateLimited</c>,
/// <c>ValidationErrors</c>, ...).
/// </summary>
public static class OutcomeCaseRegistryExtensions
{
    // ConstructOrThrow<TResponse>(PipelineEjectCaseException) below (ADR-0025) needs to close
    // IOutcomeCaseRegistry.Construct<TResponse, TCase> over a TCase known only at run
    // time (Eject.WithCase erases it from the throw site on purpose - see that type's remarks). Reflection is
    // the only way to close a generic method over a type discovered at run time; cached per (TResponse, TCase)
    // pair, mirroring DefaultOutcomeCaseRegistry's own ConstructorCache so a given (TResponse, TCase) pair
    // pays this lookup once per process, not once per eject.
    private static readonly MethodInfo ConstructMethodDefinition = typeof(IOutcomeCaseRegistry).GetMethod(
        nameof(IOutcomeCaseRegistry.Construct)
    )!;

    private static readonly ConcurrentDictionary<(Type Response, Type Case), MethodInfo> ConstructMethodCache = new();

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
    /// <c>$"A request implementing {nameof(IRequiresPolicyRequest)} must use a response type with a {nameof(Forbidden)} case"</c>).
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

    /// <summary>
    /// Constructs a <typeparamref name="TResponse"/> from a case ejected via <c>Eject.WithCase</c> (ADR-0025),
    /// or throws an <see cref="InvalidOperationException"/> if <typeparamref name="TResponse"/> has no case
    /// matching <see cref="PipelineEjectCaseException.CaseType"/> to construct.
    /// </summary>
    /// <remarks>
    /// This is the wrapper-side counterpart to the generic <see cref="ConstructOrThrow{TResponse, TCase}"/>
    /// overload above: a behavior that ejects via <c>Eject.WithCase</c> never calls either overload itself -
    /// it has no <see cref="IOutcomeCaseRegistry"/> dependency at all - so this one is called instead, exactly
    /// once, by the pipeline's dispatch machinery at the single catch site that already knows
    /// <typeparamref name="TResponse"/> concretely. Because <see cref="PipelineEjectCaseException"/>
    /// deliberately erases the case type down to a boxed <see cref="object"/> and a runtime
    /// <see cref="Type"/> (see its remarks for why), closing
    /// <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}(TCase)"/> back over that runtime type
    /// requires <see cref="MethodInfo.MakeGenericMethod"/> rather than an ordinary generic call - the cost of
    /// moving reflection out of every case-constructing behavior and into this one shared bridge, cached per
    /// (<typeparamref name="TResponse"/>, case type) pair like <see cref="ConstructOrThrow{TResponse, TCase}"/>'s
    /// own registry implementation already caches its constructor lookups.
    /// </remarks>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type to construct.</typeparam>
    /// <param name="caseRegistry">The registry to construct <typeparamref name="TResponse"/> from.</param>
    /// <param name="eject">The caught case-eject signal to construct <typeparamref name="TResponse"/> from.</param>
    /// <returns>A <typeparamref name="TResponse"/> whose active case is <see cref="PipelineEjectCaseException.CaseValue"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TResponse"/> has no case matching <see cref="PipelineEjectCaseException.CaseType"/> to construct.
    /// </exception>
    public static TResponse ConstructOrThrow<TResponse>(
        this IOutcomeCaseRegistry caseRegistry,
        PipelineEjectCaseException eject
    )
        where TResponse : IUnion
    {
        var construct = ConstructMethodCache.GetOrAdd(
            (typeof(TResponse), eject.CaseType),
            static key => ConstructMethodDefinition.MakeGenericMethod(key.Response, key.Case)
        );

        try
        {
            return (TResponse)construct.Invoke(caseRegistry, [eject.CaseValue])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is OutcomeCaseNotSupportedException inner)
        {
            throw new InvalidOperationException(
                $"Eject.WithCase ejected a {eject.CaseType.Name} case, but {typeof(TResponse).Name} has no "
                    + $"{eject.CaseType.Name} case to construct. A behavior that ejects a case via "
                    + "Eject.WithCase must be paired with a response type that declares that case.",
                inner
            );
        }
    }
}
