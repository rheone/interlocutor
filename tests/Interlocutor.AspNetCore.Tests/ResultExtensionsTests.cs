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
    public void ToActionResult_WithSuccessNoPayload_ReturnsNoContent()
    {
        // Arrange
        Outcome result = new(default(Success));

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
        var result = await behavior.Handle(
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
    public void ToActionResult_WithUnknownFailureType_Returns500()
    {
        // Arrange
        var result = new Outcome<string, int>(42);

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

    // The tests below exercise 3-arity (one success + two failure cases), 4-arity (one success + three failure
    // cases), and 5-arity (one success + four failure cases) unions, mirroring a real handler that can produce
    // several distinct failure cases - CLAUDE.md's result-type convention pushes handlers toward exactly this
    // shape rather than a single generic Failure, so the 2-arity-only coverage above doesn't prove the
    // multi-case switch works.

    [Fact]
    public void ToActionResult_ThreeArity_WithSuccess_ReturnsOkObjectResult()
    {
        // Arrange
        Outcome<string, NotFound, ValidationErrors> result = new("hello");

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
        Outcome<string, NotFound, ValidationErrors> result = new(new NotFound("Widget not found"));

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
        Outcome<string, NotFound, ValidationErrors> result = new(errors);

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
        Outcome<string, NotFound, Forbidden, ValidationErrors> result = new("hello");

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
        Outcome<string, NotFound, Forbidden, ValidationErrors> result = new(new NotFound("Widget not found"));

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
        Outcome<string, NotFound, Forbidden, ValidationErrors> result = new(new Forbidden("No access"));

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
        Outcome<string, NotFound, Forbidden, ValidationErrors> result = new(errors);

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new("hello");

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new NotFound("Widget not found"));

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(new Forbidden("No access"));

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(
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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> result = new(errors);

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
    // case types (Cancelled, Timeout, RateLimited) that DefaultHttpOutcomeStatusMapper only recently learned to
    // map. Covers success and every failure-case branch for this arity, matching what the audit-followups/05
    // arity 3-6 overloads still lacked coverage for.

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
    public void ToActionResult_SixArity_WithNotFound_Returns404()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(new NotFound("Widget not found"));

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
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(new Forbidden("No access"));

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
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(new Cancelled("caller went away"));

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
        Outcome<string, NotFound, Forbidden, Cancelled, Timeout, RateLimited> result = new(
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
