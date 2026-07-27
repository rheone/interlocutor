using Microsoft.AspNetCore.Mvc;

namespace Mediance.AspNetCore;

/// <summary>
/// The single, reusable mapping from domain outcomes to HTTP responses,
/// shared by every controller/endpoint in a consuming application. Because
/// every handler returns <see cref="Result{T}"/> or <see cref="Result"/>,
/// and both compose the same <see cref="Failure"/> union, this is the only
/// place in a consuming application HTTP status codes need to be decided.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.Value switch
        {
            T ok => new OkObjectResult(ok),
            Failure f => f.ToActionResult(),
        };

    public static IActionResult ToActionResult(this Result result) =>
        result.Value switch
        {
            Success => new NoContentResult(),
            Failure f => f.ToActionResult(),
        };

    private static IActionResult ToActionResult(this Failure failure) =>
        failure.Value switch
        {
            ValidationFailed v => new BadRequestObjectResult(
                new ValidationProblemDetails(
                    v.Errors.ToDictionary(kv => kv.Key, kv => kv.Value.Select(e => e.Message).ToArray())
                )
            ),

            RuleViolation r => new UnprocessableEntityObjectResult(
                new ProblemDetails
                {
                    Title = r.Code,
                    Detail = r.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                }
            ),

            NotFound => new NotFoundResult(),

            Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),

            Conflict c => new ConflictObjectResult(
                new ProblemDetails
                {
                    Title = c.Code,
                    Detail = c.Message,
                    Status = StatusCodes.Status409Conflict,
                }
            ),
        };
}
