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
    public void ToMinimalApiResult_WithNotFoundSingular_Returns404()
    {
        OperationOutcome<string, NotFound> result = new(new NotFound("Widget not found"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundGeneric_Returns404()
    {
        OperationOutcome<string, NotFound<int>> result = new(
            new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found")
        );
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithForbidden_Returns403()
    {
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, ValidationErrors> result = new(errors);
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithConflict_Returns409()
    {
        OperationOutcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithFailures_Returns400()
    {
        var failures = new Failures([new Failure("First failure"), new Failure("Second failure")]);
        OperationOutcome<string, Failures> result = new(failures);
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithSingleFailure_Returns400()
    {
        OperationOutcome<string, Failure> result = new(new Failure("Operation failed"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithUnknownFailureType_Returns500()
    {
        var result = new OperationOutcome<string, int>(42);
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status500InternalServerError, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithCustomMapper_UsesCustomMapping()
    {
        OperationOutcome<string, Forbidden> result = new(new Forbidden("No access"));
        var minimalResult = result.ToMinimalApiResult(new TeapotOutcomeStatusMapper());

        Assert.Equal(StatusCodes.Status418ImATeapot, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_And_ToActionResult_WithSameCustomMapper_AgreeOnStatusCode()
    {
        var mapper = new TeapotOutcomeStatusMapper();
        OperationOutcome<string, RateLimited> outcomeResult = new(new RateLimited(TimeSpan.FromSeconds(5)));

        var minimalResult = outcomeResult.ToMinimalApiResult(mapper);
        var actionResult = outcomeResult.ToActionResult(mapper);

        var minimalStatusCode = GetStatusCode(minimalResult);
        var actionStatusCode = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(actionResult).StatusCode;

        Assert.Equal(StatusCodes.Status429TooManyRequests, minimalStatusCode);
        Assert.Equal(minimalStatusCode, actionStatusCode);
    }

    // Mirrors the multi-arity coverage in ResultExtensionsTests: the same 3-arity and 5-arity outcome shapes,
    // routed through ToMinimalApiResult instead of ToActionResult, proving the two transport surfaces agree.

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithSuccess_CreatesOkResult()
    {
        OperationOutcome<string, NotFound, ValidationErrors> result = new("hello");
        var minimalResult = result.ToMinimalApiResult();

        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithNotFound_Returns404()
    {
        OperationOutcome<string, NotFound, ValidationErrors> result = new(new NotFound("Widget not found"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_ThreeArity_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required"]);
        OperationOutcome<string, NotFound, ValidationErrors> result = new(errors);
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithSuccess_CreatesOkResult()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new("hello");
        var minimalResult = result.ToMinimalApiResult();

        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithNotFound_Returns404()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new NotFound("Widget not found")
        );
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithForbidden_Returns403()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new Forbidden("No access"));
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithConflict_Returns409()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
            new Conflict("Data changed", "stale.data")
        );
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_FiveArity_WithValidationErrors_Returns422()
    {
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(errors);
        var minimalResult = result.ToMinimalApiResult();

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, GetStatusCode(minimalResult));
    }

    /// <summary>
    /// Reads the status code off a minimal API <see cref="IResult"/> via its <c>StatusCode</c> property,
    /// present on every concrete result type <see cref="ResultExtensions"/> can produce.
    /// </summary>
    private static int? GetStatusCode(IResult result) => (int?)result.GetType().GetProperty("StatusCode")?.GetValue(result);
}
