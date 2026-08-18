namespace Interlocutor.AspNetCore.Tests;

using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Verifies the status code <see cref="DefaultOutcomeStatusMapper"/> chooses for each built-in failure case,
/// independent of which transport-specific extension method renders it.
/// </summary>
public class DefaultOutcomeStatusMapperTests
{
    private readonly DefaultOutcomeStatusMapper mapper = new();

    [Fact]
    public void Map_WithNotFound_Returns404() =>
        Assert.Equal(StatusCodes.Status404NotFound, this.mapper.Map(new NotFound("missing")).StatusCode);

    [Fact]
    public void Map_WithNotFoundGeneric_Returns404() =>
        Assert.Equal(
            StatusCodes.Status404NotFound,
            this.mapper.Map(new NotFound<int>(typeof(string), 1, "missing")).StatusCode
        );

    [Fact]
    public void Map_WithForbidden_Returns403() =>
        Assert.Equal(StatusCodes.Status403Forbidden, this.mapper.Map(new Forbidden("denied")).StatusCode);

    [Fact]
    public void Map_WithConflict_Returns409() =>
        Assert.Equal(StatusCodes.Status409Conflict, this.mapper.Map(new Conflict("stale", "conflict.code")).StatusCode);

    [Fact]
    public void Map_WithValidationErrors_Returns422() =>
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, this.mapper.Map(new ValidationErrors("required")).StatusCode);

    [Fact]
    public void Map_WithFailure_Returns400NotServerError()
    {
        // See the remarks on DefaultOutcomeStatusMapper.MapFailure: a plain Failure is an expected outcome,
        // not an unhandled exception, so it must not surface as a 5xx server error.
        var mapping = this.mapper.Map(new Failure("expected domain failure"));

        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.True(mapping.StatusCode < 500);
    }

    [Fact]
    public void Map_WithFailures_Returns400NotServerError()
    {
        var mapping = this.mapper.Map(new Failures([new Failure("one"), new Failure("two")]));

        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.True(mapping.StatusCode < 500);
    }

    [Fact]
    public void Map_WithUnrecognizedCustomFailureType_Returns500()
    {
        var mapping = this.mapper.Map(new CustomRateLimited(TimeSpan.FromSeconds(1)));

        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithCustomFailureType_ViaDerivedMapper_IsOverridable()
    {
        var rateLimitAwareMapper = new RateLimitAwareOutcomeStatusMapper();

        var mapping = rateLimitAwareMapper.Map(new CustomRateLimited(TimeSpan.FromSeconds(1)));

        Assert.Equal(StatusCodes.Status429TooManyRequests, mapping.StatusCode);
    }

    [Fact]
    public void Map_WithNotFoundGeneric_ViaDerivedMapper_IsOverridable()
    {
        // NotFound<TId> can't be pattern-matched directly (it's an open generic case type), so it needs its own
        // override point - MapNotFoundGeneric - distinct from MapUnrecognized. This proves a consumer can change
        // just that case without reimplementing the reflection that detects it.
        var entityAwareMapper = new EntityNameIncludingOutcomeStatusMapper();

        var mapping = entityAwareMapper.Map(new NotFound<int>(typeof(string), 42, "missing"));

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
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(new NotFound("missing"));

        var mapping = this.mapper.Map(outcome.GetValue()!);

        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithForbiddenCase_Returns403()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(new Forbidden("denied"));

        var mapping = this.mapper.Map(outcome.GetValue()!);

        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithConflictCase_Returns409()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(
            new Conflict("stale", "conflict.code")
        );

        var mapping = this.mapper.Map(outcome.GetValue()!);

        Assert.Equal(StatusCodes.Status409Conflict, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithValidationErrorsCase_Returns422()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(
            new ValidationErrors("required")
        );

        var mapping = this.mapper.Map(outcome.GetValue()!);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, mapping.StatusCode);
    }

    [Fact]
    public void Map_AcrossFiveArityOutcome_WithSuccessCase_IsNotMappedAsFailure()
    {
        // The success case is never handed to the mapper in production - ResultExtensions branches on
        // IsSuccess before calling Map - but proving it round-trips through GetValue() confirms the
        // multi-arity outcome itself distinguishes the success case from all four failure cases correctly.
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new("ok");

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
