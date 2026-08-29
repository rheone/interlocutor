namespace Interlocutor.Abstractions.Pipeline.Cases;

using System.Collections.Immutable;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Builds an immutable <see cref="IOutcomeCaseRegistry"/>, mirroring <see cref="Classifiers.OutcomeClassifierBuilder"/>'s
/// shape: a consumer registers a custom factory and/or extractor for their own <see cref="IOperationOutcome"/>
/// implementation, and anything left unregistered falls back to <see cref="DefaultOutcomeCaseRegistry"/>'s
/// reflection-based union handling.
/// </summary>
/// <remarks>
/// Registration is needed only for response types that are <b>not</b> compiler-generated <c>union</c>
/// declarations - e.g. a hand-rolled <see cref="IOperationOutcome"/> implementation with no case constructor
/// for <see cref="Pipeline.UnionCaseFactory"/> to find reflectively, and no
/// <see cref="System.Runtime.CompilerServices.IUnion"/> marker for
/// <see cref="DefaultOutcomeCaseRegistry"/> to pattern-match. Response types that are compiler-generated
/// unions work with the default registry unmodified.
/// </remarks>
public sealed class OutcomeCaseRegistryBuilder
{
    private readonly Dictionary<(Type Response, Type Case), Delegate> _factories = [];
    private readonly Dictionary<Type, Delegate> _extractors = [];

    /// <summary>
    /// Registers a custom factory that constructs a <typeparamref name="TResponse"/> from a
    /// <typeparamref name="TCase"/> case value.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type the factory constructs.</typeparam>
    /// <typeparam name="TCase">The case type the factory accepts.</typeparam>
    /// <param name="factory">The factory to register.</param>
    /// <returns>This builder, for method chaining.</returns>
    public OutcomeCaseRegistryBuilder RegisterFactory<TResponse, TCase>(Func<TCase, TResponse> factory)
        where TResponse : IOperationOutcome
    {
        ArgumentNullException.ThrowIfNull(factory);
        this._factories[(typeof(TResponse), typeof(TCase))] = factory;
        return this;
    }

    /// <summary>
    /// Registers a custom extractor that reads the active case type/value out of a
    /// <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type the extractor handles.</typeparam>
    /// <param name="extractor">The extractor to register.</param>
    /// <returns>This builder, for method chaining.</returns>
    public OutcomeCaseRegistryBuilder RegisterExtractor<TResponse>(OutcomeCaseExtractor<TResponse> extractor)
        where TResponse : IOperationOutcome
    {
        ArgumentNullException.ThrowIfNull(extractor);
        this._extractors[typeof(TResponse)] = extractor;
        return this;
    }

    /// <summary>
    /// Builds an immutable <see cref="IOutcomeCaseRegistry"/> from the registrations.
    /// </summary>
    /// <returns>
    /// An immutable registry that applies registered factories/extractors first, falling back to
    /// <see cref="DefaultOutcomeCaseRegistry"/> for anything unregistered.
    /// </returns>
    public IOutcomeCaseRegistry Build() =>
        new CustomOutcomeCaseRegistry(this._factories.ToImmutableDictionary(), this._extractors.ToImmutableDictionary());

    /// <summary>
    /// Internal immutable registry implementation produced by the builder.
    /// </summary>
    private sealed class CustomOutcomeCaseRegistry : IOutcomeCaseRegistry
    {
        private readonly ImmutableDictionary<(Type Response, Type Case), Delegate> _factories;
        private readonly ImmutableDictionary<Type, Delegate> _extractors;

        internal CustomOutcomeCaseRegistry(
            ImmutableDictionary<(Type Response, Type Case), Delegate> factories,
            ImmutableDictionary<Type, Delegate> extractors
        )
        {
            this._factories = factories;
            this._extractors = extractors;
        }

        public TResponse Construct<TResponse, TCase>(TCase caseValue)
            where TResponse : IOperationOutcome
        {
            if (this._factories.TryGetValue((typeof(TResponse), typeof(TCase)), out var factory))
            {
                return ((Func<TCase, TResponse>)factory)(caseValue);
            }

            return DefaultOutcomeCaseRegistry.Instance.Construct<TResponse, TCase>(caseValue);
        }

        public bool TryGetActiveCase<TResponse>(TResponse response, out Type caseType, out object? caseValue)
            where TResponse : IOperationOutcome
        {
            if (response is not null && this._extractors.TryGetValue(typeof(TResponse), out var extractor))
            {
                return ((OutcomeCaseExtractor<TResponse>)extractor)(response, out caseType, out caseValue);
            }

            // DefaultOutcomeCaseRegistry itself treats a null response as "no active case" rather than a
            // configuration error, so the null-forgiving operator here just satisfies the compiler - it
            // does not assert non-null.
            return DefaultOutcomeCaseRegistry.Instance.TryGetActiveCase(response!, out caseType, out caseValue);
        }
    }
}
