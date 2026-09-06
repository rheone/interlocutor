namespace Interlocutor.Abstractions.Pipeline.Cases;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Constructs an <see cref="IUnion"/> response from one of its case values, and extracts the
/// active case type/value back out of an existing response, without either side knowing whether the
/// response is a compiler-generated union or a hand-rolled <see cref="IUnion"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// This is the general form of the reflection-based union-constructor lookup
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> already performs via <see cref="UnionCaseFactory"/>
/// to build its <c>Forbidden</c> short-circuit response. <see cref="DefaultOutcomeCaseRegistry"/> wraps that
/// same mechanism as the zero-config default; <see cref="OutcomeCaseRegistryBuilder"/> lets a consumer
/// register a custom factory/extractor pair for their own <see cref="IUnion"/> implementation
/// (one that isn't a compiler-generated <c>union</c> and so has no case constructor to find reflectively),
/// falling back to the default for anything left unregistered.
/// </para>
/// <para>
/// Both members are fail-closed: a <c>TResponse</c> this registry has no way to construct
/// or decompose throws <see cref="OutcomeCaseNotSupportedException"/> rather than returning a value that
/// silently discards the case, or returning <see langword="false"/> for what is actually a configuration
/// mistake instead of a normal "no case present" outcome.
/// </para>
/// </remarks>
public interface IOutcomeCaseRegistry
{
    /// <summary>
    /// Constructs a <typeparamref name="TResponse"/> whose active case is <paramref name="caseValue"/>.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type to construct.</typeparam>
    /// <typeparam name="TCase">The case type being constructed.</typeparam>
    /// <param name="caseValue">The case value the constructed response should carry.</param>
    /// <returns>A <typeparamref name="TResponse"/> whose active case is <paramref name="caseValue"/>.</returns>
    /// <exception cref="OutcomeCaseNotSupportedException">
    /// <typeparamref name="TResponse"/> cannot be constructed from a <typeparamref name="TCase"/> - it is
    /// neither a compiler-generated union declaring <typeparamref name="TCase"/> as a case type, nor covered
    /// by a factory registered via <see cref="OutcomeCaseRegistryBuilder"/>.
    /// </exception>
    TResponse Construct<TResponse, TCase>(TCase caseValue)
        where TResponse : IUnion;

    /// <summary>
    /// Extracts the active case type and value from <paramref name="response"/>.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type to extract from.</typeparam>
    /// <param name="response">The response to extract the active case from.</param>
    /// <param name="caseType">
    /// The runtime type of the active case, or <typeparamref name="TResponse"/> itself when this method
    /// returns <see langword="false"/> because <paramref name="response"/> was <see langword="null"/>.
    /// </param>
    /// <param name="caseValue">The active case value, or <see langword="null"/> when this method returns
    /// <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the active case was extracted; <see langword="false"/> if
    /// <paramref name="response"/> was <see langword="null"/> (there is no case to extract - not a
    /// configuration error).
    /// </returns>
    /// <exception cref="OutcomeCaseNotSupportedException">
    /// <paramref name="response"/> is non-null but is not, at run time, an <see cref="IUnion"/> instance -
    /// impossible for any legally-typed caller once <typeparamref name="TResponse"/> is itself constrained to
    /// <see cref="IUnion"/>, so this defends only against a <typeparamref name="TResponse"/> resolved outside
    /// that compile-time check (for example, closing this method reflectively over a mismatched runtime type).
    /// </exception>
    bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
        where TResponse : IUnion;
}
