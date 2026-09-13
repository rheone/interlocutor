namespace Interlocutor.AspNetCore.Tests;

using global::FluentValidation;
using global::FluentValidation.Results;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// The shipped ceiling arity (16 cases), used only by the arity-16 boundary tests in ResultExtensionsTests.
// Every type argument is distinct - required for the union's implicit-conversion-based construction to stay
// unambiguous - with RateLimited at the last slot (T16) standing in for a recognized failure case and a plain
// int at a middle slot (T9) standing in for an unrecognized one; every other slot is inert filler never
// activated by these tests.
using SixteenArityOutcome = Interlocutor.CommonKernel.Outcome<
    string,
    bool,
    double,
    long,
    short,
    byte,
    char,
    float,
    int,
    decimal,
    System.DateTime,
    System.Guid,
    System.TimeSpan,
    uint,
    ulong,
    Interlocutor.CommonKernel.ResultTypes.RateLimited
>;

/// <summary>Tests for controller-based <see cref="IActionResult"/> mapping via <see cref="ResultExtensions"/>.</summary>
public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_WithSuccessValue_ReturnsOkObjectResult()
    {
        // Arrange
        Outcome<string, Failure> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_CaseLessOutcome_ReturnsNoContent()
    {
        // Arrange - the case-less Outcome has no failure case to map, so its dedicated overload always
        // renders 204 unconditionally.
        var result = default(Outcome);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_WithNotFoundSingular_Returns404()
    {
        // Arrange
        Outcome<string, NotFound> result = new(new NotFound("Widget not found"));

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
        Outcome<string, NotFound<int>> result = new(new NotFound<int>(typeof(string), 42, "Widget with ID 42 not found"));

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
        Outcome<string, Forbidden> result = new(new Forbidden("No access"));

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
        Outcome<string, ValidationErrors> result = new(errors);

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
    public async Task ToActionResult_WithFluentValidationErrorsRoutedThroughBehavior_Returns400WithFieldKeyedValidationProblemDetails()
    {
        // Arrange - routes an actually-invalid request through a real FluentValidationErrorsBehavior (the
        // same way a mediator pipeline would), rather than constructing FluentValidationErrors by hand, so
        // this proves the field-keyed body survives the real pipeline hop into ToActionResult() end-to-end.
        var request = new CreateWidgetCommand(string.Empty, -1);
        var behavior = new FluentValidationErrorsBehavior<CreateWidgetCommand, Outcome<string, FluentValidationErrors>>(
            [new CreateWidgetCommandValidator()],
            DefaultOutcomeCaseRegistry.Instance
        );

        // Act
        var result = await behavior.HandleAsync(
            request,
            () => throw new InvalidOperationException("The handler must not run - validation should short-circuit first."),
            CancellationToken.None
        );
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var validationProblemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        var name = Assert.Single(validationProblemDetails.Errors["Name"]);
        Assert.Equal("'Name' must not be empty.", name);
        var quantity = Assert.Single(validationProblemDetails.Errors["Quantity"]);
        Assert.Equal("'Quantity' must be greater than or equal to '0'.", quantity);
    }

    [Fact]
    public void ToActionResult_WithConflict_Returns409()
    {
        // Arrange
        Outcome<string, Conflict> result = new(new Conflict("Data changed", "stale.data"));

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
        Outcome<string, Failures> result = new(failures);

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
        Outcome<string, Failure> result = new(new Failure("Operation failed"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Operation failed", problemDetails.Title);
    }

    [Fact]
    public void ToActionResult_WithUnrecognizedFailureSlotTypeAndNoMapper_ThrowsOutcomeCaseNotMappableException()
    {
        // Arrange - int occupies the failure slot (T2), but is neither the bare Success marker nor one of the
        // built-in failure case types/interfaces DefaultHttpOutcomeStatusMapper recognizes. Under the old,
        // position-based dispatch this silently fell to DefaultHttpOutcomeStatusMapper.MapUnrecognized (500) by
        // virtue of occupying a non-first slot alone; now that classification is by runtime type instead, and
        // no explicit mapper was supplied to take responsibility for it, this throws exactly as the reflective
        // catch-all already does for the equivalent unrecognized-type scenario (see
        // GenericCatchAllResultExtensionsTests) rather than guessing a status.
        var result = new Outcome<string, int>(42);

        // Act & Assert
        var exception = Assert.Throws<OutcomeCaseNotMappableException>(() => result.ToActionResult());
        Assert.Equal(typeof(Outcome<string, int>), exception.ResponseType);
        Assert.Equal(typeof(int), exception.CaseType);
    }

    [Fact]
    public void ToActionResult_WithUnrecognizedFailureSlotTypeAndExplicitMapper_UsesThatMapper()
    {
        // Arrange - an explicitly supplied mapper is always trusted for whatever type it is handed (its Map has
        // no "I don't know this type" signal of its own), so supplying one - even for a type the library's own
        // default recognition rules don't know - avoids the throw the previous test exercises.
        var result = new Outcome<string, int>(42);

        // Act
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(StatusCodes.Status418ImATeapot, statusCodeResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_WithFailureShapedValueInSuccessSlot_MapsAsFailureNotAsOk()
    {
        // Arrange - NotFound occupies the success slot (T1), an unusual but legal instantiation. The fix for
        // the "first generic-parameter position always means 200" bug: the success arm now checks the active
        // value's runtime type too, so a failure-shaped value there is still mapped as a failure instead of
        // being wrapped in an OkObjectResult just because it happened to be T1.
        Outcome<NotFound, string> result = new(new NotFound("Widget not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Widget not found", objectResult.Value);
    }

    [Fact]
    public void ToActionResult_WithCustomMapper_UsesCustomMapping()
    {
        // Arrange
        Outcome<string, Forbidden> result = new(new Forbidden("No access"));

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
        Outcome<string, CustomRateLimited> result = new(new CustomRateLimited(TimeSpan.FromSeconds(30)));

        // Act
        var actionResult = result.ToActionResult(new TeapotOutcomeStatusMapper());

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), Assert.IsType<CustomRateLimited>(objectResult.Value).RetryAfter);
    }

    // Arity 3, 4, and 5 (one success + two/three/four failure cases) used to get their own hand-duplicated
    // per-arity blocks here, proving the multi-case switch works for a real handler that can produce several
    // distinct failure cases (CLAUDE.md's result-type convention pushes handlers toward exactly this shape
    // rather than a single generic Failure). That arity-shape coverage now lives in
    // Interlocutor.Generator.Tests.Emission.ResultExtensionsEmitterTests, which is table-driven over every
    // arity 2-16 rather than re-testing the same generated switch shape by hand per arity; this file keeps the
    // arity-2/arity-6/arity-16 boundaries below as its own representative sample.

    // The tests below exercise the 6-arity overload (one success + five failure cases) - a representative
    // mid-range arity, distinct from the shipped ceiling exercised further below. WithRateLimited also covers
    // the most recently added of the three case types (Cancelled, Timeout, RateLimited) that
    // DefaultHttpOutcomeStatusMapper learned to map.

    [Fact]
    public void ToActionResult_SixArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new("hello");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_SixArity_WithRateLimited_Returns429()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
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

    // The tests below exercise the shipped ceiling arity (16 cases, see SixteenArityOutcome above) - the
    // boundary ticket aspnetcore-outcome-case-mapping-redesign/02 raised the emitter's arity ceiling to, and
    // the position an off-by-one in ResultExtensionsEmitter's loop bound is most likely to get wrong. Both
    // cases below sit away from the success slot (T1) and from each other, proving classification resolves by
    // the active case's runtime type rather than by which numbered slot happens to hold it - a recognized
    // failure (RateLimited) at the very last slot (T16) still maps to its real status, and an unrecognized
    // type (a plain int) at a middle slot (T9) still fails loudly instead of silently falling back to a
    // generic status.

    [Fact]
    public void ToActionResult_SixteenArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        SixteenArityOutcome result = "hello";

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", okResult.Value);
    }

    [Fact]
    public void ToActionResult_SixteenArity_WithRecognizedFailureAtLastCase_Returns429()
    {
        // Arrange
        SixteenArityOutcome result = new RateLimited("per-user", TimeSpan.FromSeconds(30), "slow down");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("slow down", problemDetails.Detail);
    }

    [Fact]
    public void ToActionResult_SixteenArity_WithUnrecognizedFailureSlotTypeAtMiddleCaseAndNoMapper_ThrowsOutcomeCaseNotMappableException()
    {
        // Arrange - a plain int occupying a middle failure slot (T9), matching none of the recognized failure
        // shapes, with no mapper supplied to take responsibility for it: must fail loudly rather than silently
        // mapping to a status, exactly like the arity-2 equivalent above - this proves the same refusal holds
        // at the shipped ceiling, not just at the smallest arity.
        SixteenArityOutcome result = 42;

        // Act & Assert
        var exception = Assert.Throws<OutcomeCaseNotMappableException>(() => result.ToActionResult());
        Assert.Equal(typeof(SixteenArityOutcome), exception.ResponseType);
        Assert.Equal(typeof(int), exception.CaseType);
    }

    [Fact]
    public void ToMinimalApiResult_SixteenArity_WithRecognizedFailureAtLastCase_Returns429()
    {
        // Arrange
        SixteenArityOutcome result = new RateLimited("per-user", TimeSpan.FromSeconds(30), "slow down");

        // Act
        var minimalResult = result.ToMinimalApiResult();

        // Assert
        Assert.Equal(
            StatusCodes.Status429TooManyRequests,
            (int?)minimalResult.GetType().GetProperty("StatusCode")?.GetValue(minimalResult)
        );
    }

    [Fact]
    public void ToMinimalApiResult_SixteenArity_WithUnrecognizedFailureSlotTypeAtMiddleCaseAndNoMapper_ThrowsOutcomeCaseNotMappableException()
    {
        // Arrange - the minimal-API twin of the ToActionResult test above.
        SixteenArityOutcome result = 42;

        // Act & Assert
        var exception = Assert.Throws<OutcomeCaseNotMappableException>(() => result.ToMinimalApiResult());
        Assert.Equal(typeof(SixteenArityOutcome), exception.ResponseType);
        Assert.Equal(typeof(int), exception.CaseType);
    }

    [Fact]
    public void ToActionResult_WithNullPayloadSuccess_ReturnsOkObjectResultWithNullValue()
    {
        // Arrange - a legitimately null success payload (see finding 01/07): the dedicated arity-2 overload's
        // Match-based dispatch must still route to the success branch instead of throwing or mis-mapping.
        Outcome<string?, NotFound> result = (string?)null;

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public void ToActionResult_CatchAll_WithNullPayloadSuccess_ReturnsOkObjectResultWithNullValue()
    {
        // Arrange - the reflective catch-all path (DefaultOutcomeCaseRegistry.TryGetActiveCase +
        // OutcomeClassifierBuilder.DefaultClassifier) must resolve the same null-payload success correctly,
        // not report the outcome's own container type or fail-close it to a mapped error.
        Outcome<string?, NotFound> result = (string?)null;

        // Act
        var actionResult = result.ToActionResult(caseRegistry: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Null(okResult.Value);
    }

    /// <summary>Request fixture for <see cref="ToActionResult_WithFluentValidationErrorsRoutedThroughBehavior_Returns400WithFieldKeyedValidationProblemDetails"/> only.</summary>
    private sealed record CreateWidgetCommand(string Name, int Quantity) : ICommand<Outcome<string, FluentValidationErrors>>;

    /// <summary>Validator fixture for <see cref="ToActionResult_WithFluentValidationErrorsRoutedThroughBehavior_Returns400WithFieldKeyedValidationProblemDetails"/> only.</summary>
    private sealed class CreateWidgetCommandValidator : AbstractValidator<CreateWidgetCommand>
    {
        public CreateWidgetCommandValidator()
        {
            this.RuleFor(c => c.Name).NotEmpty();
            this.RuleFor(c => c.Quantity).GreaterThanOrEqualTo(0);
        }
    }
}
