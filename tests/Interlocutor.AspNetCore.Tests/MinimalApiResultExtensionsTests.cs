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
        Outcome<string, Failure> result = new("hello");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.NotNull(minimalResult);
        Assert.Contains("Ok", minimalResult.GetType().Name);
    }

    [Fact]
    public void ToMinimalApiResult_CaseLessOutcome_CreatesNoContentResult()
    {
        // Arrange - the case-less Outcome has no failure case to map, so its dedicated overload always
        // renders 204 unconditionally.
        var result = default(Outcome);

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
        Outcome<string, NotFound> result = new(new NotFound("Widget not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithNotFoundGeneric_Returns404()
    {
        // Arrange
        Outcome<string, NotFound<int>> result = new(new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithForbidden_Returns403()
    {
        // Arrange
        Outcome<string, Forbidden> result = new(new Forbidden("No access"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithValidationErrors_Returns400()
    {
        // Arrange
        var errors = new ValidationErrors(["Name is required", "Email is invalid"]);
        Outcome<string, ValidationErrors> result = new(errors);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithConflict_Returns409()
    {
        // Arrange
        Outcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));

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
        Outcome<string, Failures> result = new(failures);

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithSingleFailure_Returns400()
    {
        // Arrange
        Outcome<string, Failure> result = new(new Failure("Operation failed"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithUnrecognizedFailureSlotTypeAndNoMapper_ThrowsOutcomeCaseNotMappableException()
    {
        // Arrange - see ResultExtensionsTests.ToActionResult_WithUnrecognizedFailureSlotTypeAndNoMapper_ThrowsOutcomeCaseNotMappableException
        // for why int, unrecognized and with no mapper supplied to take responsibility for it, now throws
        // instead of silently falling to DefaultHttpOutcomeStatusMapper.MapUnrecognized (500) by virtue of
        // occupying a non-first slot alone.
        var result = new Outcome<string, int>(42);

        // Act & Assert
        var exception = Assert.Throws<OutcomeCaseNotMappableException>(() => result.ToMinimalApiResult());
        Assert.Equal(typeof(Outcome<string, int>), exception.ResponseType);
        Assert.Equal(typeof(int), exception.CaseType);
    }

    [Fact]
    public void ToMinimalApiResult_WithFailureShapedValueInSuccessSlot_MapsAsFailureNotAsOk()
    {
        // Arrange - see ResultExtensionsTests.ToActionResult_WithFailureShapedValueInSuccessSlot_MapsAsFailureNotAsOk
        // for the same fix exercised through the minimal-API path.
        Outcome<NotFound, string> result = new(new NotFound("Widget not found"));

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, GetStatusCode(minimalResult));
    }

    [Fact]
    public void ToMinimalApiResult_WithCustomMapper_UsesCustomMapping()
    {
        // Arrange
        Outcome<string, Forbidden> result = new(new Forbidden("No access"));

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
        Outcome<string, CustomRateLimited> outcomeResult = new(new CustomRateLimited(TimeSpan.FromSeconds(5)));

        // Act
        var minimalResult = outcomeResult.ToMinimalApiResult(mapper);
        var actionResult = outcomeResult.ToActionResult(mapper);

        // Assert
        var minimalStatusCode = GetStatusCode(minimalResult);
        var actionStatusCode = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(actionResult).StatusCode;

        Assert.Equal(StatusCodes.Status429TooManyRequests, minimalStatusCode);
        Assert.Equal(minimalStatusCode, actionStatusCode);
    }

    // Arity 3, 4, and 5 used to get their own hand-duplicated blocks here, mirroring ResultExtensionsTests'
    // former per-arity coverage to prove the two transport surfaces agree at every arity. That arity-shape
    // coverage now lives in Interlocutor.Generator.Tests.Emission.ResultExtensionsEmitterTests (table-driven
    // over arities 2-16, both ToActionResult and ToMinimalApiResult); this file keeps the representative
    // arity-2 coverage and cross-transport-agreement test above, plus arity-6 and arity-16 (the shipped
    // ceiling) coverage in ResultExtensionsTests.cs, which no per-arity duplication here could add to.

    /// <summary>
    /// Reads the status code off a minimal API <see cref="IResult"/> via its <c>StatusCode</c> property,
    /// present on every concrete result type <see cref="ResultExtensions"/> can produce.
    /// </summary>
    private static int? GetStatusCode(IResult result) => (int?)result.GetType().GetProperty("StatusCode")?.GetValue(result);
}
