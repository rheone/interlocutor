using System.Diagnostics;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mediance.AspNetCore;

/// <summary>Converts Mediance <c>Result</c> and <c>Result&lt;T&gt;</c> values to ASP.NET Core <c>IActionResult</c> instances.</summary>
public static class ResultExtensions
{
    /// <summary>Converts a typed <see cref="Result{T}"/> to an <see cref="IActionResult"/> with the appropriate HTTP status code.</summary>
    /// <typeparam name="TResult">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TResult>(this Result<TResult> result) =>
        result.Value switch
        {
            TResult ok => new OkObjectResult(ok),
            Failure f => f.ToActionResult(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Converts a void <see cref="Result"/> to an <see cref="IActionResult"/> with the appropriate HTTP status code.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>A <see cref="NoContentResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult(this Result result) =>
        result.Value switch
        {
            Success => new NoContentResult(),
            Failure f => f.ToActionResult(),
            _ => throw new UnreachableException(),
        };

    private static IActionResult ToActionResult(this Failure failure) =>
        failure.Value switch
        {
            // ValidationFailed maps to 400 Bad Request with field-level errors (ValidationProblemDetails),
            // while RuleViolation maps to 422 Unprocessable Entity (request-level domain logic failure).
            ValidationFailed v => new BadRequestObjectResult(
                new ValidationProblemDetails(
                    v.errors.ToDictionary(kv => kv.Key, kv => kv.Value.Select(e => e.Message).ToArray())
                )
            ),

            ValidationFailure r => new UnprocessableEntityObjectResult(
                new ProblemDetails
                {
                    Title = r.code,
                    Detail = r.message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                }
            ),

            NotFound => new NotFoundResult(),

            Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),

            Conflict c => new ConflictObjectResult(
                new ProblemDetails
                {
                    Title = c.code,
                    Detail = c.message,
                    Status = StatusCodes.Status409Conflict,
                }
            ),

            _ => throw new UnreachableException(),
        };
}
