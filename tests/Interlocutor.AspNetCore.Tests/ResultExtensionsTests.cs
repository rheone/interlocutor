namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Tests for controller-based <see cref="IActionResult"/> mapping via <see cref="ResultExtensions"/>.</summary>
public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_WithSuccessValue_ReturnsOkObjectResult()
    {
        // Arrange
        OperationOutcome<string, Failure> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_WithSuccessNoPayload_ReturnsNoContent()
    {
        // Arrange
        OperationOutcome result = new(default(Success));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_WithNotFoundSingular_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithNotFoundGeneric_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound<int>> result = new(
            new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget with ID 42 not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithForbidden_ReturnsStatusCode403()
    {
        // Arrange
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_WithValidationErrors_Returns400()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, ValidationErrors> result = new(errors);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Contains("Name is required", problemDetails.Detail);
        Assert.Contains("Email is invalid", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_WithConflict_Returns409()
    {
        // Arrange
        OperationOutcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
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
        // Arrange
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
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
        // Arrange
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Operation failed", problemDetails.Title);
    }

    [Fact]
    public void ToActionResult_WithUnknownFailureType_Returns500()
    {
        // Arrange
        var result = new OperationOutcome<string, int>(42);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal(42, objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithCustomMapper_UsesCustomMapping()
    {
        // Arrange
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));

        // Act
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status418ImATeapot, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_WithCustomAggregateFailureType_IsMappable()
    {
        // Arrange
        OperationOutcome<string, CustomRateLimited> result = new(new CustomRateLimited(TimeSpan.FromSeconds(30)));

        // Act
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), Assert.IsType<CustomRateLimited>(objectResult.Value).RetryAfter);
    }

    // The tests below exercise 3-arity (one success + two failure cases), 4-arity (one success + three failure
    // cases), and 5-arity (one success + four failure cases) unions, mirroring a real handler that can produce
    // several distinct failure cases - CLAUDE.md's result-type convention pushes handlers toward exactly this
    // shape rather than a single generic Failure, so the 2-arity-only coverage above doesn't prove the
    // multi-case switch works.

    [Fact]
    public void ToActionResult_ThreeArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, ValidationErrors> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_ThreeArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, ValidationErrors> result = new(new NotFound("Widget not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_ThreeArity_WithValidationErrors_Returns400()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, ValidationErrors> result = new(errors);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Name is required", problemDetails.Detail);
    }

    // Auto Generated, verify expected behavior: the arity-4 ToActionResult overload (one success + three
    // failure cases) had no direct coverage anywhere in this project - only arity 2, 3, 5, and 6 were exercised.
    // These follow the exact pattern of the surrounding arity-specific tests.

    [Fact]
    public void ToActionResult_FourArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_FourArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(new NotFound("Widget not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_FourArity_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(new Forbidden("No access"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_FourArity_WithValidationErrors_Returns400()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(errors);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Name is required", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new NotFound("Widget not found")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new Forbidden("No access"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithConflict_Returns409()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new Conflict("Data changed", "stale.data")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("stale.data", problemDetails.Title);
    }

    [Fact]
    public void ToActionResult_FiveArity_WithValidationErrors_Returns400()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(errors);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
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
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_SixArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new NotFound("Widget not found")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_SixArity_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(new Forbidden("No access"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_SixArity_WithCancelled_Returns499NoBody()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new Cancelled("caller went away")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_SixArity_WithTimeout_Returns504()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new Timeout(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "downstream slow")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("downstream slow", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_SixArity_WithRateLimited_Returns429()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
            new RateLimited("per-user", TimeSpan.FromSeconds(30), "slow down")
        );

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("slow down", problemDetails.Detail);
    }
}
