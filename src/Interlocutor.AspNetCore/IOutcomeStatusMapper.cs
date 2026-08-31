namespace Interlocutor.AspNetCore;

using Interlocutor.Abstractions.Pipeline.Mapping;

/// <summary>
/// Maps a failure case value from an Interlocutor <c>Outcome</c> union to the
/// <see cref="OutcomeHttpMapping"/> (status code and optional body) that represents it over HTTP.
/// </summary>
/// <remarks>
/// <para>
/// The HTTP specialization of <see cref="IOutcomeMapper{TResult}"/> - <c>TResult</c> fixed to
/// <see cref="OutcomeHttpMapping"/> - kept as its own name here for discoverability rather than asking
/// every ASP.NET Core consumer to spell out <c>IOutcomeMapper&lt;OutcomeHttpMapping&gt;</c>.
/// </para>
/// <para>
/// Both the controller-based and minimal-API conversion helpers on <see cref="ResultExtensions"/> delegate to
/// an <see cref="IOutcomeStatusMapper"/> for every failure case, so the two transports cannot disagree about
/// what status code a given failure produces - there is exactly one place that decision is made.
/// </para>
/// <para>
/// A consumer who wants different conventions (a different status for a validation failure, a body on
/// <c>NotFound</c> where the default has none, custom handling for a domain-specific failure type) supplies
/// their own implementation - see <see cref="DefaultHttpOutcomeStatusMapper"/> for the mapping applied when
/// none is supplied - and passes it to <c>ToActionResult</c>/<c>ToMinimalApiResult</c>.
/// </para>
/// </remarks>
public interface IOutcomeStatusMapper : IOutcomeMapper<OutcomeHttpMapping>;
