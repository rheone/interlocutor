namespace Mediance.AspNetCore;

using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Converts Mediance <c>OperationOutcome</c> unions to ASP.NET Core <c>IActionResult</c> instances.</summary>
public static class ResultExtensions
{
    ///// <summary>
    ///// Converts a <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> to an <see cref="IActionResult"/>
    ///// with the appropriate HTTP status code.
    ///// </summary>
    ///// <typeparam name="TSuccess">The type of the success value.</typeparam>
    ///// <typeparam name="TFailure">The failure case type.</typeparam>
    ///// <param name="result">The result to convert.</param>
    ///// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    //public static IActionResult ToActionResult<TSuccess, TFailure>(this OperationOutcome<TSuccess, TFailure> result) =>
    //    result switch
    //    {
    //        TSuccess ok => new OkObjectResult(ok),
    //        TFailure failure => failure.ToFailureActionResult(),
    //    };

    ///// <summary>Converts a void <see cref="OperationOutcome"/> to an <see cref="IActionResult"/> with the appropriate HTTP status code.</summary>
    ///// <param name="result">The result to convert.</param>
    ///// <returns>A <see cref="NoContentResult"/> on success, or the mapped error result on failure.</returns>
    //public static IActionResult ToActionResult(this OperationOutcome result) =>
    //    result switch
    //    {
    //        Success => new NoContentResult(),
    //        Failure failure => failure.ToActionResult(),
    //    };

    ///// <summary>
    ///// Maps a failure case value to its corresponding <see cref="IActionResult"/>, special-casing the
    ///// well-known <c>Mediance.CommonKernel.ResultTypes</c> case types and falling back to a generic
    ///// 500 for anything unrecognized.
    ///// </summary>
    //private static IActionResult ToFailureActionResult<TFailure>(this TFailure failure) =>
    //    failure switch
    //    {
    //        ValidationFailure r => r.ToActionResult(),
    //        NotFound n => n.ToActionResult(),
    //        Forbidden f => f.ToActionResult(),
    //        Conflict c => c.ToActionResult(),
    //        Failure f => f.ToActionResult(),
    //        _ => new ObjectResult(failure) { StatusCode = StatusCodes.Status500InternalServerError },
    //    };

    //// ValidationFailed maps to 400 Bad Request with field-level errors (ValidationProblemDetails),
    //// while ValidationFailure maps to 422 Unprocessable Entity (request-level domain logic failure).
    //private static IActionResult ToActionResult(this ValidationFailed v) =>
    //    new BadRequestObjectResult(
    //        new ValidationProblemDetails(v.errors.ToDictionary(kv => kv.Key, kv => kv.Value.Select(e => e.Message).ToArray()))
    //    );

    //private static IActionResult ToActionResult(this ValidationFailure r) =>
    //    new UnprocessableEntityObjectResult(
    //        new ProblemDetails
    //        {
    //            Title = r.code,
    //            Detail = r.message,
    //            Status = StatusCodes.Status422UnprocessableEntity,
    //        }
    //    );

    //private static IActionResult ToActionResult(this NotFound n) => new NotFoundObjectResult(n.Message);

    //private static IActionResult ToActionResult(this Forbidden f) => new StatusCodeResult(StatusCodes.Status403Forbidden);

    //private static IActionResult ToActionResult(this Conflict c) =>
    //    new ConflictObjectResult(
    //        new ProblemDetails
    //        {
    //            Title = c.code,
    //            Detail = c.message,
    //            Status = StatusCodes.Status409Conflict,
    //        }
    //    );

    //private static IActionResult ToActionResult(this Failure f) =>
    //    new ObjectResult(
    //        new ProblemDetails { Title = f.Reason ?? "Failure", Status = StatusCodes.Status500InternalServerError }
    //    )
    //    {
    //        StatusCode = StatusCodes.Status500InternalServerError,
    //    };
}
