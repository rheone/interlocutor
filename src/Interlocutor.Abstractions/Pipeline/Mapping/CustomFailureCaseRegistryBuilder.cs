namespace Interlocutor.Abstractions.Pipeline.Mapping;

using System.Collections.Immutable;

/// <summary>
/// Builds an immutable <see cref="ICustomFailureCaseRegistry{TResult}"/> that lets a consumer register a
/// mapping rule for their own custom failure case type, keyed by exact runtime type - mirroring
/// <see cref="Classifiers.OutcomeClassifierBuilder"/>/<see cref="Cases.OutcomeCaseRegistryBuilder"/>'s builder
/// -&gt; immutable-lookup shape and <see cref="Classifiers.OutcomeClassifierBuilder.RegisterType{T}(string)"/>'s
/// two-overload registration surface.
/// </summary>
/// <typeparam name="TResult">The transport- or format-specific representation a failure case maps to.</typeparam>
/// <remarks>
/// A later registration for a type already registered overrides the earlier one (last-write-wins), matching
/// <see cref="Classifiers.OutcomeClassifierBuilder"/>'s documented override behavior.
/// </remarks>
public sealed class CustomFailureCaseRegistryBuilder<TResult>
{
    private readonly Dictionary<Type, Delegate> _mappings = [];

    /// <summary>
    /// Registers a mapping rule for the custom failure case type <typeparamref name="TFailure"/>.
    /// </summary>
    /// <typeparam name="TFailure">The custom failure case type to register a mapping rule for.</typeparam>
    /// <param name="mapping">The mapping rule, invoked with the failure value.</param>
    /// <returns>This builder, for method chaining.</returns>
    /// <remarks>
    /// A second call for a <typeparamref name="TFailure"/> already registered overrides the first.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="mapping"/> is <see langword="null"/>.</exception>
    public CustomFailureCaseRegistryBuilder<TResult> RegisterType<TFailure>(Func<TFailure, TResult> mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        this._mappings[typeof(TFailure)] = mapping;
        return this;
    }

    /// <summary>
    /// Registers a mapping rule for a custom failure case type, by runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The custom failure case type to register a mapping rule for.</param>
    /// <param name="mapping">
    /// The mapping rule, invoked with the failure value. Must be a <see cref="Func{T, TResult}"/> closed over
    /// <paramref name="type"/> and <typeparamref name="TResult"/>.
    /// </param>
    /// <returns>This builder, for method chaining.</returns>
    /// <remarks>
    /// A second call for a <paramref name="type"/> already registered overrides the first.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="mapping"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="mapping"/> is not a <see cref="Func{T, TResult}"/> closed over <paramref name="type"/>
    /// and <typeparamref name="TResult"/>.
    /// </exception>
    public CustomFailureCaseRegistryBuilder<TResult> RegisterType(Type type, Delegate mapping)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(mapping);

        var expectedDelegateType = typeof(Func<,>).MakeGenericType(type, typeof(TResult));
        if (mapping.GetType() != expectedDelegateType)
        {
            throw new ArgumentException(
                $"{nameof(mapping)} must be a Func<{type.Name}, {typeof(TResult).Name}> delegate matching "
                    + $"{nameof(type)}, but was a {mapping.GetType().Name}.",
                nameof(mapping)
            );
        }

        this._mappings[type] = mapping;
        return this;
    }

    /// <summary>
    /// Builds an immutable registry from the registrations.
    /// </summary>
    /// <returns>
    /// An immutable, thread-safe registry that looks up a mapping rule by exact runtime type, matching
    /// "not found" with <see langword="false"/> rather than throwing.
    /// </returns>
    public ICustomFailureCaseRegistry<TResult> Build() =>
        new DefaultCustomFailureCaseRegistry(this._mappings.ToImmutableDictionary());

    /// <summary>
    /// Internal immutable registry implementation produced by the builder.
    /// </summary>
    private sealed class DefaultCustomFailureCaseRegistry : ICustomFailureCaseRegistry<TResult>
    {
        private readonly ImmutableDictionary<Type, Delegate> _mappings;

        internal DefaultCustomFailureCaseRegistry(ImmutableDictionary<Type, Delegate> mappings)
        {
            this._mappings = mappings;
        }

        /// <inheritdoc/>
        public bool TryMap<TFailure>(TFailure failure, out TResult result)
        {
            if (this._mappings.TryGetValue(typeof(TFailure), out var mapping))
            {
                result = ((Func<TFailure, TResult>)mapping)(failure);
                return true;
            }

            result = default!;
            return false;
        }
    }
}
