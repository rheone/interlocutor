namespace Interlocutor.AspNetCore.Tests;

using global::FluentValidation.Results;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Verifies the status code <see cref="DefaultOutcomeStatusMapper"/> chooses for each built-in failure case,
/// independent of which transport-specific extension method renders it.
/// </summary>
public class DefaultOutcomeStatusMapperTests
{
    private readonly DefaultOutcomeStatusMapper mapper = new();

    [Fact]
    public void Map_WithNotFound_Returns404()
    {
        // Arrange
        var notFound = new NotFound("missing");

        // Act
        var mapping = this.mapper.Map(notFound);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithNotFoundGeneric_Returns404()
    {
        // Arrange
        var notFound = new NotFound<int>(typeof(string), 1, "missing");

        // Act
        var mapping = this.mapper.Map(notFound);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithForbidden_Returns403()
    {
        // Arrange
        var forbidden = new Forbidden("denied");

        // Act
        var mapping = this.mapper.Map(forbidden);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithConflict_Returns409()
    {
        // Arrange
        var conflict = new Conflict("stale", "conflict.code");

        // Act
        var mapping = this.mapper.Map(conflict);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithValidationErrors_Returns400()
    {
        // Arrange
        var validationErrors = new ValidationErrors("required");

        // Act
        var mapping = this.mapper.Map(validationErrors);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithFluentValidationErrors_Returns400()
    {
        // Arrange - a real second IValidationFailureCase implementer, not the library's own ValidationErrors.
        var fluentValidationErrors = new FluentValidationErrors([
            new ValidationFailure("Email", "'Email' is not a valid email address."),
        ]);

        // Act
        var mapping = this.mapper.Map(fluentValidationErrors);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(mapping.Body);
        Assert.Equal("'Email' is not a valid email address.", problemDetails.Detail);
    }

    [Fact]
    public void Map_WithHandRolledValidationFailureCase_Returns400()
    {
        // Arrange - proves DefaultOutcomeStatusMapper recognizes IValidationFailureCase via the interface
        // itself, not a type-specific special case for the library's own or FluentValidation's types.
        var customCase = new CustomValidationFailureCase(["custom rule failed"]);

        // Act
        var mapping = this.mapper.Map(customCase);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(mapping.Body);
        Assert.Equal("custom rule failed", problemDetails.Detail);
    }

    [Fact]
    public void Map_WithValidationErrors_MappingIsUnaffectedByIValidationFailureCaseRecognition()
    {
        // Arrange - the exact ValidationErrors branch in Map<TFailure> is ordered before the new
        // IValidationFailureCase branch, so this mapping is byte-for-byte the same as before that branch existed.
        var validationErrors = new ValidationErrors(["required"]);

        // Act
        var mapping = this.mapper.Map(validationErrors);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(mapping.Body);
        Assert.Equal("Validation Failed", problemDetails.Title);
        Assert.Equal("required", problemDetails.Detail);
    }

    [Fact]
    public void Map_WithCancelled_Returns499NoBody()
    {
        // Arrange
        var cancelled = new Cancelled("caller went away");

        // Act
        var mapping = this.mapper.Map(cancelled);

        // Assert
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, mapping.StatusCode);
        Assert.Null(mapping.Body);
    }

    [Fact]
    public void Map_WithTimeout_Returns504()
    {
        // Arrange
        var timeout = new Timeout(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "downstream slow");

        // Act
        var mapping = this.mapper.Map(timeout);

        // Assert
        Assert.Equal(StatusCodes.Status504GatewayTimeout, mapping.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(mapping.Body);
        Assert.Equal("downstream slow", problemDetails.Detail);
    }

    [Fact]
    public void Map_WithRateLimited_Returns429()
    {
        // Arrange
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "slow down");

        // Act
        var mapping = this.mapper.Map(rateLimited);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, mapping.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(mapping.Body);
        Assert.Equal("slow down", problemDetails.Detail);
    }

    [Fact]
    public void Map_WithFailure_Returns400NotServerError()
    {
        // Arrange - see the remarks on DefaultOutcomeStatusMapper.MapFailure: a plain Failure is an expected
        // outcome, not an unhandled exception, so it must not surface as a 5xx server error.
        var failure = new Failure("expected domain failure");

        // Act
        var mapping = this.mapper.Map(failure);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.True(mapping.StatusCode < 500);
    }

    [Fact]
    public void Map_WithFailures_Returns400NotServerError()
    {
        // Arrange
        var failures = new Failures([new Failure("one"), new Failure("two")]);

        // Act
        var mapping = this.mapper.Map(failures);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.True(mapping.StatusCode < 500);
    }

    [Fact]
    public void Map_WithUnrecognizedCustomFailureType_Returns500()
    {
        // Arrange
        var customFailure = new CustomRateLimited(TimeSpan.FromSeconds(1));

        // Act
        var mapping = this.mapper.Map(customFailure);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithCustomFailureType_ViaDerivedMapper_IsOverridable()
    {
        // Arrange
        var rateLimitAwareMapper = new RateLimitAwareOutcomeStatusMapper();

        // Act
        var mapping = rateLimitAwareMapper.Map(new CustomRateLimited(TimeSpan.FromSeconds(1)));

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithNotFoundGeneric_ViaDerivedMapper_IsOverridable()
    {
        // Arrange - NotFound<TId> can't be pattern-matched directly (it's an open generic case type), so it
        // needs its own override point - MapNotFoundGeneric - distinct from MapUnrecognized. This proves a
        // consumer can change just that case without reimplementing the reflection that detects it.
        var entityAwareMapper = new EntityNameIncludingOutcomeStatusMapper();

        // Act
        var mapping = entityAwareMapper.Map(new NotFound<int>(typeof(string), 42, "missing"));

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
        Assert.Equal("String/42", mapping.Body);
    }

    // The tests below build a single 5-arity outcome type - one success case plus four distinct failure
    // cases - and confirm the mapper picks the right status code for whichever case the outcome actually
    // holds. This proves per-case selection generalizes beyond the 2-arity shape exercised above, matching
    // a handler that reports several distinct failure cases from one operation.

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithNotFoundCase_Returns404()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(new NotFound("missing"));

        // Act
        var mapping = this.mapper.Map(outcome.GetValue()!);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithForbiddenCase_Returns403()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(new Forbidden("denied"));

        // Act
        var mapping = this.mapper.Map(outcome.GetValue()!);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithConflictCase_Returns409()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(
            new Conflict("stale", "conflict.code")
        );

        // Act
        var mapping = this.mapper.Map(outcome.GetValue()!);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithValidationErrorsCase_Returns400()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(
            new ValidationErrors("required")
        );

        // Act
        var mapping = this.mapper.Map(outcome.GetValue()!);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithSuccessCase_IsNotMappedAsFailure()
    {
        // Arrange - the success case is never handed to the mapper in production - ResultExtensions branches
        // on IsSuccess before calling Map - but proving it round-trips through GetValue() confirms the
        // multi-arity outcome itself distinguishes the success case from all four failure cases correctly.
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new("ok");

        // Act & Assert
        Assert.True(outcome.IsSuccess);
        Assert.Equal("ok", outcome.GetValue());
    }

    /// <summary>
    /// Proves the "overridable default" contract: derive from <see cref="DefaultOutcomeStatusMapper"/> and
    /// override just the one case that needs to change, inheriting every other mapping unchanged.
    /// </summary>
    private sealed class RateLimitAwareOutcomeStatusMapper : DefaultOutcomeStatusMapper
    {
        protected override OutcomeHttpMapping MapUnrecognized<TFailure>(TFailure failure) =>
            failure is CustomRateLimited rateLimited
                ? new OutcomeHttpMapping(StatusCodes.Status429TooManyRequests, rateLimited)
                : base.MapUnrecognized(failure);
    }

    /// <summary>Proves <see cref="DefaultOutcomeStatusMapper.MapNotFoundGeneric"/> is independently overridable.</summary>
    private sealed class EntityNameIncludingOutcomeStatusMapper : DefaultOutcomeStatusMapper
    {
        protected override OutcomeHttpMapping MapNotFoundGeneric(Type entityType, object? id, string? message) =>
            new(StatusCodes.Status404NotFound, $"{entityType.Name}/{id}");
    }
}
