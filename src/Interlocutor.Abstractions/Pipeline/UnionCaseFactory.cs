namespace Interlocutor.Abstractions.Pipeline;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// Resolves and invokes the constructor the C# union feature generates for one case type of a union, so a
/// short-circuiting behavior can construct its failure case for a caller-chosen response type whose concrete
/// shape isn't known at compile time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> and
/// <c>Interlocutor.FluentValidation.ValidationBehavior&lt;TRequest, TResponse&gt;</c> both need this: neither
/// can be generic over a success-type parameter with its failure case pinned at compile time (e.g.
/// <c>Outcome&lt;TSuccess, Forbidden&gt;</c>) without breaking Microsoft.Extensions.DependencyInjection's
/// open-generic resolution - see <see cref="AuthorizationBehavior{TRequest, TResponse}"/>'s remarks for the
/// full explanation. Both are instead generic directly over <c>TResponse</c>, constrained only to
/// <see cref="CommonKernel.ResultTypes.IOutcome"/>, and use this factory to construct the one case they need.
/// </para>
/// <para>
/// A union type's compiler-generated constructors are ordinary CLR constructors - one per declared case type,
/// each accepting that case type as its sole parameter - so <see cref="FindCaseConstructor{TResponse, TCase}"/>
/// is exact-signature reflection, not a heuristic: it can never ambiguously match the wrong case, because two
/// case types producing the same closed constructor signature would already fail to compile as duplicate
/// members on the union type itself.
/// </para>
/// </remarks>
public static class UnionCaseFactory
{
    /// <summary>
    /// Finds <typeparamref name="TResponse"/>'s constructor accepting a single <typeparamref name="TCase"/>
    /// argument - the case constructor the union feature generates when <typeparamref name="TResponse"/>
    /// declares <typeparamref name="TCase"/> as one of its case types.
    /// </summary>
    /// <typeparam name="TResponse">The union type to search.</typeparam>
    /// <typeparam name="TCase">The case type whose constructor to find.</typeparam>
    /// <returns>
    /// The matching constructor, or <see langword="null"/> if <typeparamref name="TResponse"/> has no case of
    /// type <typeparamref name="TCase"/>. Callers typically resolve this once per closed
    /// <typeparamref name="TResponse"/> and cache it in a <see langword="static readonly"/> field - a distinct
    /// field per generic instantiation, so the cost is paid at most once per response shape actually used.
    /// </returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2090:RequiresUnreferencedCode",
        Justification = "TResponse is always a union type the caller's own code constructs or pattern-matches "
            + "on elsewhere (a compile-time Outcome<...> instantiation), so the trimmer already keeps "
            + "its compiler-generated case constructors reachable independent of this reflective lookup. "
            + "Requiring DynamicallyAccessedMembers(PublicConstructors) here would be viral across every "
            + "pipeline behavior generic over TResponse - see documentation/trim-and-aot-support.md."
    )]
    public static ConstructorInfo? FindCaseConstructor<TResponse, TCase>() => typeof(TResponse).GetConstructor([typeof(TCase)]);

    /// <summary>
    /// Invokes a case constructor previously resolved by <see cref="FindCaseConstructor{TResponse, TCase}"/>,
    /// constructing the union's <typeparamref name="TCase"/> case from <paramref name="caseValue"/>.
    /// </summary>
    /// <typeparam name="TResponse">The union type being constructed.</typeparam>
    /// <typeparam name="TCase">The case type being constructed.</typeparam>
    /// <param name="caseConstructor">
    /// The constructor returned by a prior <see cref="FindCaseConstructor{TResponse, TCase}"/> call. Must not
    /// be <see langword="null"/> - callers are expected to have already handled the no-such-case case with
    /// their own specific, actionable error message before calling this.
    /// </param>
    /// <param name="caseValue">The case value to construct <typeparamref name="TResponse"/> from.</param>
    /// <returns>The constructed <typeparamref name="TResponse"/>.</returns>
    public static TResponse Invoke<TResponse, TCase>(ConstructorInfo caseConstructor, TCase caseValue) =>
        (TResponse)caseConstructor.Invoke([caseValue]);
}
