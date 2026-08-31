namespace Interlocutor.Abstractions.Pipeline.Mapping;

/// <summary>
/// Maps a failure case value from an Interlocutor <c>Outcome</c> union to a
/// <typeparamref name="TResult"/> representing it in some transport or output format - an HTTP status and
/// body, a gRPC status, a process exit code, a queue nack reason, or anything else a consumer needs to
/// derive from a failure case.
/// </summary>
/// <typeparam name="TResult">The transport- or format-specific representation a failure case maps to.</typeparam>
/// <remarks>
/// <para>
/// This is the transport-agnostic shape <c>Interlocutor.AspNetCore.IOutcomeStatusMapper</c> specializes for
/// HTTP (<typeparamref name="TResult"/> fixed to <c>OutcomeHttpMapping</c>). It lives here, free of any
/// dependency on a particular transport, so a non-HTTP consumer - a gRPC service, a CLI tool, a queue
/// handler - gets the same pluggable per-failure-case mapping ergonomic without hand-rolling their own
/// dispatch over the built-in failure cases.
/// </para>
/// <para>
/// A concrete implementation for a given <typeparamref name="TResult"/> normally derives from
/// <see cref="OutcomeMapperBase{TResult}"/>, which supplies the dispatch over each built-in failure case,
/// rather than implementing this interface directly.
/// </para>
/// <para>
/// Covariant in <typeparamref name="TResult"/>: it only ever appears in a return position here, never as a
/// parameter, so an <c>IOutcomeMapper&lt;Derived&gt;</c> is safely usable wherever an
/// <c>IOutcomeMapper&lt;Base&gt;</c> is expected.
/// </para>
/// </remarks>
public interface IOutcomeMapper<out TResult>
{
    /// <summary>
    /// Maps a failure case value to its <typeparamref name="TResult"/> representation.
    /// </summary>
    /// <typeparam name="TFailure">
    /// The static failure case type carried by the <c>Outcome</c> - a built-in case type
    /// (<c>NotFound</c>, <c>Forbidden</c>, <c>Conflict</c>, ...), one of their aggregate forms
    /// (<c>ValidationErrors</c>, <c>Failures</c>), or a consumer-defined custom failure type.
    /// </typeparam>
    /// <param name="failure">The failure case value produced by the handler or pipeline.</param>
    /// <returns>The <typeparamref name="TResult"/> representing <paramref name="failure"/>.</returns>
    TResult Map<TFailure>(TFailure failure);
}
