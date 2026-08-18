namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_WithSuccessValue_ReturnsOkObjectResult()
    {
        OperationOutcome<string, Failure> result = new("hello");
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_WithSuccessNoPayload_ReturnsNoContent()
    {
        OperationOutcome result = new(default(Success));
        var actionResult = result.ToActionResult();

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_WithNotFoundSingular_Returns404()
    {
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithNotFoundGeneric_Returns404()
    {
        OperationOutcome<string, NotFound<int>> result = new(
            new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget with ID 42 not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithForbidden_ReturnsStatusCode403()
    {
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));
        var actionResult = result.ToActionResult();

        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, ValidationErrors> result = new(errors);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problemDetails.Status);
        Assert.Contains("Name is required", problemDetails.Detail);
        Assert.Contains("Email is invalid", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_WithConflict_Returns409()
    {
        OperationOutcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("stale.data", problemDetails.Title);
        Assert.Equal("Data changed", problemDetails.Detail);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
    }

    [Fact]
    public void ToActionResult_WithFailures_Returns400()
    {
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Contains("First failure", problemDetails.Detail);
        Assert.Contains("Second failure", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_WithSingleFailure_Returns400()
    {
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Operation failed", problemDetails.Title);
    }

    [Fact]
    public void ToActionResult_WithUnknownFailureType_Returns500()
    {
        var result = new OperationOutcome<string, int>(42);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal(42, objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithCustomMapper_UsesCustomMapping()
    {
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status418ImATeapot, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_WithCustomAggregateFailureType_IsMappable()
    {
        OperationOutcome<string, CustomRateLimited> result = new(new CustomRateLimited(TimeSpan.FromSeconds(30)));
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), Assert.IsType<CustomRateLimited>(objectResult.Value).RetryAfter);
    }

    // The tests below exercise 3-arity (one success + two failure cases) and 5-arity (one success + four
    // failure cases) unions, mirroring a real handler that can produce several distinct failure cases -
    // CLAUDE.md's result-type convention pushes handlers toward exactly this shape rather than a single
    // generic Failure, so the 2-arity-only coverage above doesn't prove the multi-case switch works.

    [Fact]
    public void ToActionResult_ThreeArity_WithSuccess_ReturnsOkObjectResult()
    {
        OperationOutcome<string, NotFound, ValidationErrors> result = new("hello");
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_ThreeArity_WithNotFound_Returns404()
    {
        OperationOutcome<string, NotFound, ValidationErrors> result = new(new NotFound("Widget not found"));
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_ThreeArity_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, ValidationErrors> result = new(errors);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Name is required", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithSuccess_ReturnsOkObjectResult()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new("hello");
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithNotFound_Returns404()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new NotFound("Widget not found")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithForbidden_Returns403()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new Forbidden("No access"));
        var actionResult = result.ToActionResult();

        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithConflict_Returns409()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new Conflict("Data changed", "stale.data")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("stale.data", problemDetails.Title);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(errors);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Name is required", problemDetails.Detail);
        Assert.Contains("Email is invalid", problemDetails.Detail);
    }

    // The tests below exercise the 6-arity overload (one success + five failure cases), including the three
    // case types (Cancelled, Timeout, RateLimited) that DefaultOutcomeStatusMapper only recently learned to
    // map. Covers success and every failure-case branch for this arity, matching what the audit-followups/05
    // arity 3-6 overloads still lacked coverage for.

    [Fact]
    public void ToActionResult_SixArity_WithSuccess_ReturnsOkObjectResult()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new("hello");
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_SixArity_WithNotFound_Returns404()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new NotFound("Widget not found")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_SixArity_WithForbidden_Returns403()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(new Forbidden("No access"));
        var actionResult = result.ToActionResult();

        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_SixArity_WithCancelled_Returns499NoBody()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new Cancelled("caller went away")
        );
        var actionResult = result.ToActionResult();

        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_SixArity_WithTimeout_Returns408()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new Timeout(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "downstream slow")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status408RequestTimeout, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("downstream slow", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_SixArity_WithRateLimited_Returns429()
    {
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new RateLimited("per-user", TimeSpan.FromSeconds(30), "slow down")
        );
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("slow down", problemDetails.Detail);
    }
}
