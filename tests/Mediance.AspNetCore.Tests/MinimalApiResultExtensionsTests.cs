namespace Mediance.AspNetCore.Tests;

using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

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
        OperationOutcome<string, NotFound<int>> result = new(
            new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found")
        );
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
