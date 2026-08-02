namespace Mediance.AspNetCore;

using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Converts Mediance <c>OperationOutcome</c> unions to ASP.NET Core result types.
/// Provides both controller-based (<see cref="IActionResult"/>) and minimal API mappings,
/// sharing the same status-code decision logic to prevent drift.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> to an <see cref="IActionResult"/>
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TFailure>(this OperationOutcome<TSuccess, TFailure> result) =>
        result switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TFailure failure => failure.ToFailureActionResult(),
        };

    /// <summary>Converts a void <see cref="OperationOutcome"/> to an <see cref="IActionResult"/> with the appropriate HTTP status code.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>A <see cref="NoContentResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult(this OperationOutcome result) =>
        result switch
        {
            Success => new NoContentResult(),
            Failure failure => failure.ToActionResult(),
        };

    /// <summary>
    /// Maps a failure case value to its corresponding <see cref="IActionResult"/>, special-casing the
    /// well-known <c>Mediance.CommonKernel.ResultTypes</c> case types and falling back to a generic
    /// 500 for anything unrecognized.
    /// </summary>
    private static IActionResult ToFailureActionResult<TFailure>(this TFailure failure)
    {
        // Pattern matching doesn't work well with generic types like NotFound<T>, so we use
        // a combination of pattern matching and type checking via the type itself.
        if (failure is ValidationErrors validationErrors)
        {
            return validationErrors.ToActionResult();
        }

        if (failure is Forbidden forbidden)
        {
            return forbidden.ToActionResult();
        }

        if (failure is Conflict conflict)
        {
            return conflict.ToActionResult();
        }

        if (failure is Failures failures)
        {
            return failures.ToActionResult();
        }

        if (failure is Failure singleFailure)
        {
            return singleFailure.ToActionResult();
        }

        // Check for NotFound (both singular and generic)
        var failureType = typeof(TFailure);
        if (failureType == typeof(NotFound))
        {
            return ((NotFound)(object)failure!).ToActionResult();
        }

        // Check for NotFound<TId> - handles any generic NotFound type
        if (failureType.IsGenericType && failureType.GetGenericTypeDefinition() == typeof(NotFound<>))
        {
            // Handle generic NotFound<T> - extract the Message property via reflection
            var messageProperty = failureType.GetProperty("Message");
            var message = (string?)messageProperty?.GetValue(failure);
            return new NotFoundObjectResult(message);
        }

        return new ObjectResult(failure) { StatusCode = StatusCodes.Status500InternalServerError };
    }

    /// <summary>Maps a ValidationErrors aggregate to 422 Unprocessable Entity with all validation errors.</summary>
    private static IActionResult ToActionResult(this ValidationErrors v) =>
        new UnprocessableEntityObjectResult(
            new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", v.Messages),
                Status = StatusCodes.Status422UnprocessableEntity,
            }
        );

    /// <summary>Maps a NotFound singular case to 404 Not Found.</summary>
    private static IActionResult ToActionResult(this NotFound n) => new NotFoundObjectResult(n.Message);

    /// <summary>Maps a Forbidden case to 403 Forbidden.</summary>
    private static IActionResult ToActionResult(this Forbidden f) => new StatusCodeResult(StatusCodes.Status403Forbidden);

    /// <summary>Maps a Conflict case to 409 Conflict with error details.</summary>
    private static IActionResult ToActionResult(this Conflict c) =>
        new ConflictObjectResult(
            new ProblemDetails
            {
                Title = c.Code,
                Detail = c.Message,
                Status = StatusCodes.Status409Conflict,
            }
        );

    /// <summary>Maps a Failures aggregate to 500 Internal Server Error with all failure reasons.</summary>
    private static IActionResult ToActionResult(this Failures f) =>
        new ObjectResult(
            new ProblemDetails
            {
                Title = "Multiple Failures",
                Detail = string.Join("; ", f.Values.Select(failure => failure.Reason ?? "Failure")),
                Status = StatusCodes.Status500InternalServerError,
            }
        )
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };

    /// <summary>Maps a Failure case to 500 Internal Server Error.</summary>
    private static IActionResult ToActionResult(this Failure f) =>
        new ObjectResult(
            new ProblemDetails { Title = f.Reason ?? "Failure", Status = StatusCodes.Status500InternalServerError }
        )
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };

    // ============================================================================
    // Minimal API Mappings
    // ============================================================================
    // These mappings mirror the controller-based mappings above, using the same
    // status-code decisions to ensure both endpoints produce identical results.

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> to a minimal API result
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TFailure>(this OperationOutcome<TSuccess, TFailure> result) =>
        result switch
        {
            TSuccess ok => Results.Ok(ok),
            TFailure failure => failure.ToFailureMinimalApiResult(),
        };

    /// <summary>Converts a void <see cref="OperationOutcome"/> to a minimal API result with the appropriate HTTP status code.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>HTTP 204 No Content on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult(this OperationOutcome result) =>
        result switch
        {
            Success => Results.NoContent(),
            Failure failure => failure.ToFailureMinimalApiResult(),
        };

    /// <summary>
    /// Maps a failure case value to its corresponding minimal API result, sharing the same status
    /// codes and details as the controller-based mappings.
    /// </summary>
    private static IResult ToFailureMinimalApiResult<TFailure>(this TFailure failure)
    {
        if (failure is ValidationErrors validationErrors)
        {
            return ToMinimalApiResult(validationErrors);
        }

        if (failure is Forbidden forbidden)
        {
            return ToMinimalApiResult(forbidden);
        }

        if (failure is Conflict conflict)
        {
            return ToMinimalApiResult(conflict);
        }

        if (failure is Failures failures)
        {
            return ToMinimalApiResult(failures);
        }

        if (failure is Failure singleFailure)
        {
            return ToMinimalApiResult(singleFailure);
        }

        // Check for NotFound (both singular and generic)
        var failureType = typeof(TFailure);
        if (failureType == typeof(NotFound))
        {
            return ToMinimalApiResult((NotFound)(object)failure!);
        }

        // Check for NotFound<TId> - handles any generic NotFound type
        if (failureType.IsGenericType && failureType.GetGenericTypeDefinition() == typeof(NotFound<>))
        {
            var messageProperty = failureType.GetProperty("Message");
            var message = (string?)messageProperty?.GetValue(failure);
            return Results.NotFound(message);
        }

        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    /// <summary>Maps a ValidationErrors aggregate to 422 Unprocessable Entity with all validation errors.</summary>
    private static IResult ToMinimalApiResult(this ValidationErrors v) =>
        Results.UnprocessableEntity(
            new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = string.Join("; ", v.Messages),
                Status = StatusCodes.Status422UnprocessableEntity,
            }
        );

    /// <summary>Maps a NotFound singular case to 404 Not Found.</summary>
    private static IResult ToMinimalApiResult(this NotFound n) => Results.NotFound(n.Message);

    /// <summary>Maps a Forbidden case to 403 Forbidden.</summary>
    private static IResult ToMinimalApiResult(this Forbidden f) => Results.StatusCode(StatusCodes.Status403Forbidden);

    /// <summary>Maps a Conflict case to 409 Conflict with error details.</summary>
    private static IResult ToMinimalApiResult(this Conflict c) =>
        Results.Conflict(
            new ProblemDetails
            {
                Title = c.Code,
                Detail = c.Message,
                Status = StatusCodes.Status409Conflict,
            }
        );

    /// <summary>Maps a Failures aggregate to 500 Internal Server Error with all failure reasons.</summary>
    private static IResult ToMinimalApiResult(this Failures f) => Results.StatusCode(StatusCodes.Status500InternalServerError);

    /// <summary>Maps a Failure case to 500 Internal Server Error.</summary>
    private static IResult ToMinimalApiResult(this Failure f) => Results.StatusCode(StatusCodes.Status500InternalServerError);
}
