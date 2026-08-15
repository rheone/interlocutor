namespace Interlocutor.AspNetCore;

using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The <see cref="IOutcomeStatusMapper"/> applied by <see cref="ResultExtensions"/> when a consumer registers
/// none of their own. Recognizes the built-in <c>Interlocutor.CommonKernel.ResultTypes</c> failure cases and
/// their aggregate forms; anything else — including a consumer's own custom failure type — falls back to
/// <see cref="MapUnrecognized{TFailure}"/>.
/// </summary>
/// <remarks>
/// Each case is a protected virtual method, so a consumer who wants to change how one case maps while keeping
/// the rest can derive from this class and override just that method, rather than reimplementing
/// <see cref="IOutcomeStatusMapper"/> from scratch.
/// </remarks>
public class DefaultOutcomeStatusMapper : IOutcomeStatusMapper
{
    /// <inheritdoc/>
    public OutcomeHttpMapping Map<TFailure>(TFailure failure) =>
        failure switch
        {
            ValidationErrors validationErrors => this.MapValidationErrors(validationErrors),
            Forbidden forbidden => this.MapForbidden(forbidden),
            Conflict conflict => this.MapConflict(conflict),
            Failures failures => this.MapFailures(failures),
            Failure singleFailure => this.MapFailure(singleFailure),
            NotFound notFound => this.MapNotFound(notFound),
            _ => this.MapNotFoundGenericOrUnrecognized(failure),
        };

    /// <summary>Maps a <see cref="ValidationErrors"/> aggregate to 422 Unprocessable Entity with all messages.</summary>
    /// <param name="validationErrors">The validation failures to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    protected virtual OutcomeHttpMapping MapValidationErrors(ValidationErrors validationErrors) =>
        new(
            StatusCodes.Status422UnprocessableEntity,
            new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", validationErrors.Messages),
                Status = StatusCodes.Status422UnprocessableEntity,
            }
        );

    /// <summary>Maps a <see cref="NotFound"/> singular case to 404 Not Found.</summary>
    /// <param name="notFound">The not-found failure to map.</param>
    /// <returns>The mapped status code and message body.</returns>
    protected virtual OutcomeHttpMapping MapNotFound(NotFound notFound) => new(StatusCodes.Status404NotFound, notFound.Message);

    /// <summary>Maps a <see cref="Forbidden"/> case to 403 Forbidden, with no body (the reason is not exposed to callers).</summary>
    /// <param name="forbidden">The forbidden failure to map.</param>
    /// <returns>The mapped status code, with no body.</returns>
    protected virtual OutcomeHttpMapping MapForbidden(Forbidden forbidden) => new(StatusCodes.Status403Forbidden);

    /// <summary>Maps a <see cref="Conflict"/> case to 409 Conflict with its code and message.</summary>
    /// <param name="conflict">The conflict failure to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    protected virtual OutcomeHttpMapping MapConflict(Conflict conflict) =>
        new(
            StatusCodes.Status409Conflict,
            new ProblemDetails
            {
                Title = conflict.Code,
                Detail = conflict.Message,
                Status = StatusCodes.Status409Conflict,
            }
        );

    /// <summary>
    /// Maps a <see cref="Failures"/> aggregate to 400 Bad Request with all failure reasons.
    /// </summary>
    /// <param name="failures">The failures to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="Failures"/> aggregates ordinary <see cref="Failure"/> instances — expected outcomes, never
    /// unhandled exceptions (see <see cref="MapFailure"/> for the same reasoning applied to the singular case).
    /// </para>
    /// </remarks>
    protected virtual OutcomeHttpMapping MapFailures(Failures failures) =>
        new(
            StatusCodes.Status400BadRequest,
            new ProblemDetails
            {
                Title = "Multiple Failures",
                Detail = string.Join("; ", failures.Values.Select(failure => failure.Reason ?? "Failure")),
                Status = StatusCodes.Status400BadRequest,
            }
        );

    /// <summary>
    /// Maps a <see cref="Failure"/> case to 400 Bad Request.
    /// </summary>
    /// <param name="failure">The failure to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// <para>
    /// <b>Deliberately not 500.</b> <see cref="Failure"/> is the library's generic fallback for an ordinary
    /// <em>expected</em> outcome a handler chose to report — CONTEXT.md is explicit that it "never represents an
    /// unexpected exception; exceptions signal defects, not expected outcomes." A prior version of this mapper
    /// mapped it to 500 Internal Server Error, which tells the caller (and any alerting built on status codes)
    /// that the server malfunctioned, when in fact the handler completed normally and reported a domain failure.
    /// 400 Bad Request reflects that the request could not be satisfied for a reason the caller can act on,
    /// without implying a server fault. Unhandled exceptions still surface as 500 through ASP.NET Core's normal
    /// exception-handling middleware — this mapper is only ever invoked for outcomes a handler returned on
    /// purpose.
    /// </para>
    /// </remarks>
    protected virtual OutcomeHttpMapping MapFailure(Failure failure) =>
        new(
            StatusCodes.Status400BadRequest,
            new ProblemDetails { Title = failure.Reason ?? "Failure", Status = StatusCodes.Status400BadRequest }
        );

    /// <summary>
    /// Maps an unrecognized failure case, including the generic <c>NotFound&lt;TId&gt;</c> variant and any
    /// consumer-defined custom failure type.
    /// </summary>
    /// <typeparam name="TFailure">The static failure case type.</typeparam>
    /// <param name="failure">The failure value to map.</param>
    /// <returns>
    /// For <c>NotFound&lt;TId&gt;</c>, the same mapping as <see cref="MapNotFound"/>. For anything else,
    /// 500 Internal Server Error with the failure value itself as the body — this mapper was never taught what
    /// the failure means, so it cannot claim a more specific status; a consumer with a custom failure type should
    /// override this method (or supply their own <see cref="IOutcomeStatusMapper"/>) to map it precisely.
    /// </returns>
    protected virtual OutcomeHttpMapping MapUnrecognized<TFailure>(TFailure failure) =>
        new(StatusCodes.Status500InternalServerError, failure);

    private OutcomeHttpMapping MapNotFoundGenericOrUnrecognized<TFailure>(TFailure failure)
    {
        // Pattern matching doesn't work against the open generic NotFound<> case type, so fall back to
        // reflection: check whether TFailure closes NotFound<TId> for any TId, then read its Message.
        var failureType = typeof(TFailure);
        if (failureType.IsGenericType && failureType.GetGenericTypeDefinition() == typeof(NotFound<>))
        {
            var messageProperty = failureType.GetProperty("Message");
            var message = (string?)messageProperty?.GetValue(failure);
            return new OutcomeHttpMapping(StatusCodes.Status404NotFound, message);
        }

        return this.MapUnrecognized(failure);
    }
}
