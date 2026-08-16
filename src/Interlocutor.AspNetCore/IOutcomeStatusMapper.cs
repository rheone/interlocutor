namespace Interlocutor.AspNetCore;

/// <summary>
/// Maps a failure case value from an Interlocutor <c>OperationOutcome</c> union to the
/// <see cref="OutcomeHttpMapping"/> (status code and optional body) that represents it over HTTP.
/// </summary>
/// <remarks>
/// <para>
/// Both the controller-based and minimal-API conversion helpers on <see cref="ResultExtensions"/> delegate to
/// an <see cref="IOutcomeStatusMapper"/> for every failure case, so the two transports cannot disagree about
/// what status code a given failure produces - there is exactly one place that decision is made.
/// </para>
/// <para>
/// A consumer who wants different conventions (a different status for a validation failure, a body on
/// <c>NotFound</c> where the default has none, custom handling for a domain-specific failure type) supplies
/// their own implementation - see <see cref="DefaultOutcomeStatusMapper"/> for the mapping applied when none
/// is supplied - and passes it to <c>ToActionResult</c>/<c>ToMinimalApiResult</c>.
/// </para>
/// </remarks>
public interface IOutcomeStatusMapper
{
    /// <summary>
    /// Maps a failure case value to the HTTP status code and optional body that should represent it.
    /// </summary>
    /// <typeparam name="TFailure">
    /// The static failure case type carried by the <c>OperationOutcome</c> - a built-in case type
    /// (<c>NotFound</c>, <c>Forbidden</c>, <c>Conflict</c>, ...), one of their aggregate forms
    /// (<c>ValidationErrors</c>, <c>Failures</c>), or a consumer-defined custom failure type.
    /// </typeparam>
    /// <param name="failure">The failure case value produced by the handler or pipeline.</param>
    /// <returns>The status code and optional body to use for the HTTP response.</returns>
    OutcomeHttpMapping Map<TFailure>(TFailure failure);
}
