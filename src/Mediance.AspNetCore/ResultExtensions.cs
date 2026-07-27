using System.Diagnostics;
using Mediance.CommonKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mediance.AspNetCore;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.Value switch
        {
            T ok => new OkObjectResult(ok),
            Failure f => f.ToActionResult(),
            _ => throw new UnreachableException(),
        };

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

            _ => throw new UnreachableException(),
        };
}
