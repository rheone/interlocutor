namespace Interlocutor.Abstractions.Pipeline.Mapping;

/// <summary>
/// Looks up a consumer-registered mapping rule for a custom failure case type, by exact runtime type - the
/// immutable lookup <see cref="CustomFailureCaseRegistryBuilder{TResult}"/> produces.
/// </summary>
/// <typeparam name="TResult">The transport- or format-specific representation a failure case maps to.</typeparam>
/// <remarks>
/// <para>
/// This is the same builder -&gt; immutable-lookup shape <see cref="Classifiers.OutcomeClassifierBuilder"/> and
/// <see cref="Cases.OutcomeCaseRegistryBuilder"/> already use for their own, differently-shaped registries: a
/// consumer accumulates registrations on a builder, then <see cref="CustomFailureCaseRegistryBuilder{TResult}.Build"/>
/// freezes them into an immutable, thread-safe lookup keyed by exact runtime type - no predicate or inheritance
/// matching, matching those registries' exact-type-key dictionaries.
/// </para>
/// <para>
/// A consumer builds one of these and supplies it to <see cref="OutcomeMapperBase{TResult}"/>'s constructor;
/// <see cref="OutcomeMapperBase{TResult}.Map{TFailure}"/> consults it after every built-in case and before
/// falling back to <see cref="OutcomeMapperBase{TResult}.MapUnrecognized{TFailure}"/>. A registry is also usable
/// standalone, independent of <see cref="OutcomeMapperBase{TResult}"/>, via <see cref="TryMap{TFailure}"/> directly.
/// </para>
/// </remarks>
public interface ICustomFailureCaseRegistry<TResult>
{
    /// <summary>
    /// Attempts to map <paramref name="failure"/> using the mapping rule registered for its exact runtime
    /// type, <typeparamref name="TFailure"/>.
    /// </summary>
    /// <typeparam name="TFailure">The static failure case type to look up a mapping rule for.</typeparam>
    /// <param name="failure">The failure value to map.</param>
    /// <param name="result">
    /// The mapped <typeparamref name="TResult"/>, when a mapping rule was registered for
    /// <typeparamref name="TFailure"/>; otherwise the default value of <typeparamref name="TResult"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a mapping rule was registered for <typeparamref name="TFailure"/> and
    /// <paramref name="result"/> was produced from it; <see langword="false"/> if no mapping rule is
    /// registered for <typeparamref name="TFailure"/> - not an error, just a "not found" signal for the
    /// caller to fall back on.
    /// </returns>
    bool TryMap<TFailure>(TFailure failure, out TResult result);
}
