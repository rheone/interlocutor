namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;

/// <summary>Tests for minimal API result mapping, mirroring the controller-based tests.</summary>
public class MinimalApiResultExtensionsTests
{
    [Fact]
    public void ToMinimalApiResult_WithSuccessValue_CreatesOkResult()
    {
        // Arrange
        OperationOutcome<string, Failure> result = new("hello");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.NotNull(minimalResult);
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithSuccessNoPayload_CreatesNoContentResult()
    {
        // Arrange
        OperationOutcome result = new(default(Success));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.NotNull(minimalResult);
        Assert.Equal("NoContent", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundSingular_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundGeneric_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound<int>> result = new(
            new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found")
        );

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithValidationErrors_Returns422()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, ValidationErrors> result = new(errors);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithConflict_Returns409()
    {
        // Arrange
        OperationOutcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithFailures_Returns400()
    {
        // Arrange
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithSingleFailure_Returns400()
    {
        // Arrange
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithUnknownFailureType_Returns500()
    {
        // Arrange
        var result = new OperationOutcome<string, int>(42);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithCustomMapper_UsesCustomMapping()
    {
        // Arrange
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));

        // Act
        var minimalResult = result.ToMinimalApiResult(new TeapotOutcomeStatusMapper());

        // Assert
        Assert.Equal(StatusCodes.Status418ImATeapot, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_And_ToActionResult_WithSameCustomMapper_AgreeOnStatusCode()
    {
        // Arrange
        var mapper = new TeapotOutcomeStatusMapper();
        OperationOutcome<string, CustomRateLimited> outcomeResult = new(new CustomRateLimited(TimeSpan.FromSeconds(5)));

        // Act
        var minimalResult = outcomeResult.ToMinimalApiResult(mapper);
        var actionResult = outcomeResult.ToActionResult(mapper);

        // Assert
        var minimalStatusCode = GetStatusCode(minimalResult);
        var actionStatusCode = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(actionResult).StatusCode;

        Assert.Equal(StatusCodes.Status429TooManyRequests, minimalStatusCode);
        Assert.Equal(minimalStatusCode, actionStatusCode);
    }

    // Mirrors the multi-arity coverage in ResultExtensionsTests: the same 3-arity, 4-arity, and 5-arity outcome
    // shapes, routed through ToMinimalApiResult instead of ToActionResult, proving the two transport surfaces
    // agree.

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithSuccess_CreatesOkResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, ValidationErrors> result = new("hello");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, ValidationErrors> result = new(new NotFound("Widget not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithValidationErrors_Returns422()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, ValidationErrors> result = new(errors);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    // Auto Generated, verify expected behavior: the arity-4 ToMinimalApiResult overload (one success + three
    // failure cases) had no direct coverage anywhere in this project - only arity 2, 3, 5, and 6 were exercised.
    // These follow the exact pattern of the surrounding arity-specific tests.

    [Fact]
    public void ToMinimalApiResult_FourArity_WithSuccess_CreatesOkResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new("hello");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_FourArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(new NotFound("Widget not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FourArity_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(new Forbidden("No access"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FourArity_WithValidationErrors_Returns422()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, Forbidden, ValidationErrors> result = new(errors);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithSuccess_CreatesOkResult()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new("hello");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithNotFound_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new NotFound("Widget not found")
        );

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithForbidden_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new Forbidden("No access"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithConflict_Returns409()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new Conflict("Data changed", "stale.data")
        );

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithValidationErrors_Returns422()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(errors);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    /// <summary>
    /// Reads the status code off a minimal API <see cref="IResult"/> via its <c>StatusCode</c> property,
    /// present on every concrete result type <see cref="ResultExtensions"/> can produce.
    /// </summary>
    private static int? GetStatusCode(IResult result) => (int?)result.GetType().GetProperty("StatusCode")?.GetValue(result);
}
