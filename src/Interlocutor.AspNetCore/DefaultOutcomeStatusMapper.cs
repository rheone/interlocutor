namespace Interlocutor.AspNetCore;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The <see cref="IOutcomeStatusMapper"/> applied by <see cref="ResultExtensions"/> when a consumer registers
/// none of their own. Recognizes the built-in <c>Interlocutor.CommonKernel.ResultTypes</c> failure cases and
/// their aggregate forms; anything else - including a consumer's own custom failure type - falls back to
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
            IValidationFailureCase validationFailureCase => this.MapValidationFailureCase(validationFailureCase),
            Forbidden forbidden => this.MapForbidden(forbidden),
            Conflict conflict => this.MapConflict(conflict),
            Failures failures => this.MapFailures(failures),
            Failure singleFailure => this.MapFailure(singleFailure),
            NotFound notFound => this.MapNotFound(notFound),
            Cancelled cancelled => this.MapCancelled(cancelled),
            Timeout timeout => this.MapTimeout(timeout),
            RateLimited rateLimited => this.MapRateLimited(rateLimited),
            _ => this.MapNotFoundGenericOrUnrecognized(failure),
        };

    /// <summary>Maps a <see cref="ValidationErrors"/> aggregate to 400 Bad Request with all messages.</summary>
    /// <param name="validationErrors">The validation failures to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    protected virtual OutcomeHttpMapping MapValidationErrors(ValidationErrors validationErrors) =>
        MapValidationMessages(validationErrors.Messages);

    /// <summary>
    /// Maps any <see cref="IValidationFailureCase"/> implementer other than <see cref="ValidationErrors"/>
    /// itself to 400 Bad Request with its messages - notably
    /// <c>Interlocutor.FluentValidation.FluentValidationErrors</c>, and any future implementer this mapper
    /// was never taught about directly.
    /// </summary>
    /// <param name="validationFailureCase">The validation failure to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// Ordered after the exact <see cref="ValidationErrors"/> branch in <see cref="Map{TFailure}"/>, so a
    /// plain <see cref="ValidationErrors"/> failure still resolves to <see cref="MapValidationErrors"/>
    /// unchanged - this branch only ever runs for some other <see cref="IValidationFailureCase"/> implementer.
    /// </remarks>
    protected virtual OutcomeHttpMapping MapValidationFailureCase(IValidationFailureCase validationFailureCase) =>
        MapValidationMessages(validationFailureCase.Messages);

    /// <summary>
    /// Builds the 400 Bad Request mapping shared by <see cref="MapValidationErrors"/> and
    /// <see cref="MapValidationFailureCase"/> - both produce an identical <see cref="ProblemDetails"/> shape,
    /// differing only in where their messages come from.
    /// </summary>
    /// <param name="messages">The validation failure messages to join into the body's <c>Detail</c>.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// 400, not 422: 422 Unprocessable Entity is a narrower WebDAV-originated code for a syntactically valid
    /// but semantically un-processable request, and most HTTP clients/tooling only special-case 400. A prior
    /// version of this mapper used 422 for validation failures specifically; this uses the same 400 Bad
    /// Request every other malformed-request-shaped failure in this mapper already uses, so a caller doesn't
    /// need to special-case validation to detect "the request was rejected."
    /// </remarks>
    private static OutcomeHttpMapping MapValidationMessages(IReadOnlyList<string> messages) =>
        new(
            StatusCodes.Status400BadRequest,
            new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", messages),
                Status = StatusCodes.Status400BadRequest,
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

    /// <summary>Maps a <see cref="Cancelled"/> case to a client-closed-request status, with no body (the reason is diagnostic, not for callers).</summary>
    /// <param name="cancelled">The cancellation failure to map.</param>
    /// <returns>The mapped status code, with no body.</returns>
    /// <remarks>
    /// The status this maps to is deliberately not 400 Bad Request: cancellation is neither a malformed request
    /// nor a domain failure - the caller withdrew interest in the operation before it completed. See
    /// <c>aspnetcore-integration.md</c> for the specific status code this maps to.
    /// </remarks>
    protected virtual OutcomeHttpMapping MapCancelled(Cancelled cancelled) => new(StatusCodes.Status499ClientClosedRequest);

    /// <summary>Maps a <see cref="Timeout"/> case to a gateway-timeout status with its budget and elapsed time.</summary>
    /// <param name="timeout">The timeout failure to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// 504 Gateway Timeout, not 408 Request Timeout: 408 means the <em>client</em> took too long sending its
    /// request, which is not what <see cref="Timeout"/> represents - it's the server exceeding its own time
    /// budget waiting on an operation (a downstream call, a handler). 504 is the code for exactly that: an
    /// upstream/dependency the server was waiting on didn't respond within budget. A prior version of this
    /// mapper used 408 here, which told the caller they themselves were slow when they weren't.
    /// </remarks>
    protected virtual OutcomeHttpMapping MapTimeout(Timeout timeout) =>
        new(
            StatusCodes.Status504GatewayTimeout,
            new ProblemDetails
            {
                Title = "Timeout",
                Detail = timeout.Message ?? $"Budget={timeout.Budget}, Elapsed={timeout.Elapsed}",
                Status = StatusCodes.Status504GatewayTimeout,
            }
        );

    /// <summary>Maps a <see cref="RateLimited"/> case to a too-many-requests status with the limiter that was hit.</summary>
    /// <param name="rateLimited">The rate-limit failure to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// Does not set a <c>Retry-After</c> response header from <see cref="RateLimited.RetryAfter"/> - this mapper
    /// produces a transport-agnostic <see cref="OutcomeHttpMapping"/> with no notion of response headers. A
    /// consumer who needs the header can override this method (or read <see cref="RateLimited.RetryAfter"/> from
    /// the body) to set it.
    /// </remarks>
    protected virtual OutcomeHttpMapping MapRateLimited(RateLimited rateLimited) =>
        new(
            StatusCodes.Status429TooManyRequests,
            new ProblemDetails
            {
                Title = "Rate Limited",
                Detail = rateLimited.Message ?? rateLimited.LimitKey,
                Status = StatusCodes.Status429TooManyRequests,
            }
        );

    /// <summary>
    /// Maps a <see cref="Failures"/> aggregate to 400 Bad Request with all failure reasons.
    /// </summary>
    /// <param name="failures">The failures to map.</param>
    /// <returns>The mapped status code and <see cref="ProblemDetails"/> body.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="Failures"/> aggregates ordinary <see cref="Failure"/> instances - expected outcomes, never
    /// unhandled exceptions (see <see cref="MapFailure"/> for the same reasoning applied to the singular case).
    /// </para>
    /// </remarks>
    protected virtual OutcomeHttpMapping MapFailures(Failures failures) =>
        new(
            StatusCodes.Status400BadRequest,
            new ProblemDetails
            {
                Title = "Multiple Failures",
                Detail = string.Join("; ", failures.Reasons),
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
    /// <em>expected</em> outcome a handler chose to report - CONTEXT.md is explicit that it "never represents an
    /// unexpected exception; exceptions signal defects, not expected outcomes." A prior version of this mapper
    /// mapped it to 500 Internal Server Error, which tells the caller (and any alerting built on status codes)
    /// that the server malfunctioned, when in fact the handler completed normally and reported a domain failure.
    /// 400 Bad Request reflects that the request could not be satisfied for a reason the caller can act on,
    /// without implying a server fault. Unhandled exceptions still surface as 500 through ASP.NET Core's normal
    /// exception-handling middleware - this mapper is only ever invoked for outcomes a handler returned on
    /// purpose.
    /// </para>
    /// </remarks>
    protected virtual OutcomeHttpMapping MapFailure(Failure failure) =>
        new(
            StatusCodes.Status400BadRequest,
            new ProblemDetails { Title = failure.Reason ?? "Failure", Status = StatusCodes.Status400BadRequest }
        );

    /// <summary>Maps the generic <c>NotFound&lt;TId&gt;</c> case to 404 Not Found.</summary>
    /// <param name="entityType">The <see cref="Type"/> of entity that was not found.</param>
    /// <param name="id">The identifier that was searched for, boxed - its static type varies per closed <c>NotFound&lt;TId&gt;</c>.</param>
    /// <param name="message">The optional human-readable message carried by the <c>NotFound&lt;TId&gt;</c> instance.</param>
    /// <returns>The mapped status code and message body.</returns>
    /// <remarks>
    /// A separate override point from <see cref="MapNotFound"/> because pattern matching can't target the open
    /// generic <c>NotFound&lt;&gt;</c> case type directly - <see cref="MapNotFoundGenericOrUnrecognized{TFailure}"/>
    /// reflects the closed instance's fields and forwards them here, so a consumer who wants to change only this
    /// case's mapping still overrides a plain virtual method rather than reimplementing the reflection.
    /// </remarks>
    protected virtual OutcomeHttpMapping MapNotFoundGeneric(Type entityType, object? id, string? message) =>
        new(StatusCodes.Status404NotFound, message);

    /// <summary>
    /// Maps a failure case this mapper was never taught, including any consumer-defined custom failure type.
    /// </summary>
    /// <typeparam name="TFailure">The static failure case type.</typeparam>
    /// <param name="failure">The failure value to map.</param>
    /// <returns>
    /// 500 Internal Server Error with the failure value itself as the body - this mapper cannot claim a more
    /// specific status for a type it doesn't recognize; a consumer with a custom failure type should override
    /// this method (or supply their own <see cref="IOutcomeStatusMapper"/>) to map it precisely.
    /// </returns>
    protected virtual OutcomeHttpMapping MapUnrecognized<TFailure>(TFailure failure) =>
        new(StatusCodes.Status500InternalServerError, failure);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2090:RequiresUnreferencedCode",
        Justification = "The only closed generic ever tested here is Interlocutor.CommonKernel.ResultTypes."
            + "NotFound<TId> - a library-owned type whose EntityType/Id/Message properties are always kept by "
            + "the trimmer independent of this reflective read, since every consumer that produces a NotFound<> "
            + "failure also constructs or pattern-matches it directly elsewhere. See "
            + "documentation/trim-and-aot-support.md."
    )]
    private OutcomeHttpMapping MapNotFoundGenericOrUnrecognized<TFailure>(TFailure failure)
    {
        // Pattern matching doesn't work against the open generic NotFound<> case type, so fall back to
        // reflection: check whether TFailure closes NotFound<TId> for any TId, then read its fields and
        // forward to the overridable MapNotFoundGeneric rather than building the mapping here directly.
        var failureType = typeof(TFailure);
        if (failureType.IsGenericType && failureType.GetGenericTypeDefinition() == typeof(NotFound<>))
        {
            var entityType = (Type)failureType.GetProperty("EntityType")!.GetValue(failure)!;
            var id = failureType.GetProperty("Id")?.GetValue(failure);
            var message = (string?)failureType.GetProperty("Message")?.GetValue(failure);
            return this.MapNotFoundGeneric(entityType, id, message);
        }

        return this.MapUnrecognized(failure);
    }
}
