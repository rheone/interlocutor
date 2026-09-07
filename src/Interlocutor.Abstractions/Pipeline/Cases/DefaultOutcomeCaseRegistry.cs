namespace Interlocutor.Abstractions.Pipeline.Cases;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Zero-config <see cref="IOutcomeCaseRegistry"/> that handles any <see cref="IUnion"/> response which is a
/// compiler-generated <c>union</c>: construction via <see cref="UnionCaseFactory"/>'s case-constructor
/// reflection, extraction via pattern-matching the compiler-generated <see cref="IUnion"/> marker interface.
/// </summary>
/// <remarks>
/// This is what <see cref="OutcomeCaseRegistryBuilder"/> falls back to for any response type a consumer has
/// not registered a custom factory or extractor for - use <see cref="Instance"/> directly when no custom
/// <see cref="IUnion"/> implementations are in play.
/// </remarks>
public sealed class DefaultOutcomeCaseRegistry : IOutcomeCaseRegistry
{
    // Case constructor lookup is resolved reflectively once per (TResponse, TCase) pair and cached here for
    // the lifetime of the process, mirroring UnionCaseFactory's own per-generic-instantiation caching but
    // shared across every closed generic Construct<TResponse, TCase> call rather than requiring a distinct
    // static field per instantiation.
    private static readonly ConcurrentDictionary<(Type Response, Type Case), ConstructorInfo?> ConstructorCache = new();

    /// <summary>Gets the shared default registry instance.</summary>
    /// <value>The shared default registry instance.</value>
    public static IOutcomeCaseRegistry Instance { get; } = new DefaultOutcomeCaseRegistry();

    /// <inheritdoc/>
    public TResponse Construct<TResponse, TCase>(TCase caseValue)
        where TResponse : IUnion
    {
        var constructor = ConstructorCache.GetOrAdd(
            (typeof(TResponse), typeof(TCase)),
            static _ => UnionCaseFactory.FindCaseConstructor<TResponse, TCase>()
        );

        if (constructor is null)
        {
            throw new OutcomeCaseNotSupportedException(
                typeof(TResponse),
                typeof(TCase),
                $"Cannot construct {typeof(TResponse).Name} from a {typeof(TCase).Name} case: {typeof(TResponse).Name} "
                    + $"has no {typeof(TCase).Name} case to construct. A response type used with {nameof(IOutcomeCaseRegistry)} "
                    + $"must either be a compiler-generated union declaring {typeof(TCase).Name} as one of its case types, "
                    + $"or have a factory registered for it via {nameof(OutcomeCaseRegistryBuilder)}."
            );
        }

        return UnionCaseFactory.Invoke<TResponse, TCase>(constructor, caseValue);
    }

    /// <inheritdoc/>
    public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
        where TResponse : IUnion
    {
        if (response is null)
        {
            caseType = typeof(TResponse);
            caseValue = null;
            return false;
        }

        if (response is IUnion union)
        {
            caseValue = union.Value;
            if (caseValue is not null)
            {
                caseType = caseValue.GetType();
                return true;
            }

            // A null Value alone can't say whether this is a legitimate null case payload or a genuinely
            // unset union (see finding 07) - IOutcomeCase.ActiveCaseType resolves that ambiguity from inside
            // the union itself, where the declared case types are known even when the boxed value isn't.
            if (response.AsOutcomeCaseActiveType() is { } activeCaseType)
            {
                caseType = activeCaseType;
                return true;
            }

            // Genuinely unset union: no case was ever constructed.
            caseType = typeof(TResponse);
            return false;
        }

        // Unreachable for any TResponse resolved through the ordinary generic API: the IOutcomeCaseRegistry
        // constraint on TResponse is IUnion itself, so a legally-typed, non-null response already satisfied
        // the check above. Left in place (unchanged in behavior, per this ticket) as a defense against a
        // TResponse resolved outside that compile-time check - e.g. this method closed reflectively over a
        // mismatched runtime type.
        throw new OutcomeCaseNotSupportedException(
            typeof(TResponse),
            null,
            $"Cannot extract the active case from {typeof(TResponse).Name}: it is not a compiler-generated union "
                + $"(it does not implement {nameof(IUnion)}). A response type used with {nameof(IOutcomeCaseRegistry)} "
                + $"must either be a compiler-generated union, or have an extractor registered for it via "
                + $"{nameof(OutcomeCaseRegistryBuilder)}."
        );
    }
}
