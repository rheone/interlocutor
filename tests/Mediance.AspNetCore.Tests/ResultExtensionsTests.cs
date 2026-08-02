namespace Mediance.AspNetCore.Tests;

using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
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
    public void ToActionResult_WithNotFoundSingular_ReturnsNotFoundResult()
    {
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));
        var actionResult = result.ToActionResult();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal("Widget not found", notFoundResult.Value);
    }

    [Fact]
    public void ToActionResult_WithNotFoundGeneric_ReturnsNotFoundResult()
    {
        OperationOutcome<string, NotFound<int>> result = new(new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found"));
        var actionResult = result.ToActionResult();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal("Widget with ID 42 not found", notFoundResult.Value);
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

        var objectResult = Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
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

        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal("stale.data", problemDetails.Title);
        Assert.Equal("Data changed", problemDetails.Detail);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
    }

    [Fact]
    public void ToActionResult_WithFailures_Returns500()
    {
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Contains("First failure", problemDetails.Detail);
        Assert.Contains("Second failure", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_WithSingleFailure_Returns500()
    {
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));
        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
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
}

/// <summary>Tests for minimal API result mapping, mirroring the controller-based tests.</summary>
public class MinimalApiResultExtensionsTests
{
    [Fact]
    public void ToMinimalApiResult_WithSuccessValue_CreatesOkResult()
    {
        OperationOutcome<string, Failure> result = new("hello");
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithSuccessNoPayload_CreatesNoContentResult()
    {
        OperationOutcome result = new(default(Success));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Equal("NoContent", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundSingular_CreatesNotFoundResult()
    {
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("NotFound", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundGeneric_CreatesNotFoundResult()
    {
        OperationOutcome<string, NotFound<int>> result = new(new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("NotFound", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithForbidden_CreatesStatusCodeResult()
    {
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("StatusCode", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithValidationErrors_CreatesUnprocessableEntityResult()
    {
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, ValidationErrors> result = new(errors);
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("UnprocessableEntity", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithConflict_CreatesConflictResult()
    {
        OperationOutcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("Conflict", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithFailures_CreatesStatusCodeResult()
    {
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("StatusCode", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithSingleFailure_CreatesStatusCodeResult()
    {
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("StatusCode", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithUnknownFailureType_CreatesStatusCodeResult()
    {
        var result = new OperationOutcome<string, int>(42);
        var minimalResult = result.ToMinimalApiResult();

        Assert.NotNull(minimalResult);
        Assert.Contains("StatusCode", minimalResult.GetType().Name);
    }
}
