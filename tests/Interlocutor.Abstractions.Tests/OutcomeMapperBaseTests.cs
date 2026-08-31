namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Mapping;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Verifies <see cref="OutcomeMapperBase{TResult}"/>'s per-case dispatch against a non-HTTP
/// <c>TResult</c> - a process exit code - proving the abstraction is genuinely usable outside an HTTP
/// context, not merely a renamed copy of <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c>.
/// </summary>
public class OutcomeMapperBaseTests
{
    private readonly ExitCodeOutcomeMapper mapper = new();

    [Fact]
    public void Map_WithNotFound_ReturnsNotFoundExitCode()
    {
        // Arrange
        var notFound = new NotFound("missing");

        // Act
        var exitCode = this.mapper.Map(notFound);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.NotFoundExitCode, exitCode);
    }

    [Fact]
    public void Map_WithNotFoundGeneric_ReturnsNotFoundExitCode()
    {
        // Arrange - NotFound<TId> can't be pattern-matched directly (it's an open generic case type), so
        // dispatch has to reach it via the base's reflective fallback rather than the switch in Map<TFailure>.
        var notFound = new NotFound<int>(typeof(string), 1, "missing");

        // Act
        var exitCode = this.mapper.Map(notFound);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.NotFoundExitCode, exitCode);
    }

    [Fact]
    public void Map_WithForbidden_ReturnsForbiddenExitCode()
    {
        // Arrange
        var forbidden = new Forbidden("denied");

        // Act
        var exitCode = this.mapper.Map(forbidden);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.ForbiddenExitCode, exitCode);
    }

    [Fact]
    public void Map_WithValidationErrors_ReturnsValidationExitCode()
    {
        // Arrange
        var validationErrors = new ValidationErrors("required");

        // Act
        var exitCode = this.mapper.Map(validationErrors);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.ValidationExitCode, exitCode);
    }

    [Fact]
    public void Map_WithOtherValidationFailureCaseImplementer_ReturnsValidationExitCode()
    {
        // Arrange - proves recognition happens via the IValidationFailureCase interface itself, not a
        // type-specific branch for the library's own ValidationErrors.
        var customCase = new StubValidationFailureCase(["custom rule failed"]);

        // Act
        var exitCode = this.mapper.Map(customCase);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.ValidationExitCode, exitCode);
    }

    [Fact]
    public void Map_WithUnrecognizedCustomFailureType_ReturnsUnknownExitCode()
    {
        // Arrange
        var customFailure = new StubCustomFailure("boom");

        // Act
        var exitCode = this.mapper.Map(customFailure);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.UnknownExitCode, exitCode);
    }

    [Fact]
    public void Map_AcrossMultiArityOutcome_SelectsMatchingCase()
    {
        // Arrange - proves per-case selection generalizes beyond a single failure type, matching a handler
        // that reports several distinct failure cases from one operation.
        Outcome<string, NotFound, Forbidden, Conflict, ValidationErrors> outcome = new(new Conflict("stale", "conflict.code"));

        // Act
        var exitCode = this.mapper.Map(outcome.GetValue()!);

        // Assert
        Assert.Equal(ExitCodeOutcomeMapper.ConflictExitCode, exitCode);
    }

    [Fact]
    public void Map_WithCustomFailureType_ViaDerivedMapper_IsOverridable()
    {
        // Arrange - proves the "overridable default" contract survives genericization: derive one level
        // further and override just the one case that needs to change.
        var rateLimitAwareMapper = new RateLimitAwareExitCodeOutcomeMapper();

        // Act
        var exitCode = rateLimitAwareMapper.Map(new StubCustomFailure("throttled"));

        // Assert
        Assert.Equal(42, exitCode);
    }

    /// <summary>A hand-rolled <see cref="IValidationFailureCase"/> implementer, not one of the kernel's own types.</summary>
    private readonly record struct StubValidationFailureCase(IReadOnlyList<string> Messages) : IValidationFailureCase;

    /// <summary>A failure type this mapper was never taught about.</summary>
    private readonly record struct StubCustomFailure(string Reason);

    /// <summary>A trivial <see cref="OutcomeMapperBase{TResult}"/> for a non-HTTP <c>TResult</c> - a process exit code.</summary>
    private class ExitCodeOutcomeMapper : OutcomeMapperBase<int>
    {
        public const int NotFoundExitCode = 2;
        public const int ForbiddenExitCode = 3;
        public const int ConflictExitCode = 4;
        public const int ValidationExitCode = 5;
        public const int UnknownExitCode = 1;

        protected override int MapNotFound(NotFound notFound) => NotFoundExitCode;

        protected override int MapNotFoundGeneric(Type entityType, object? id, string? message) => NotFoundExitCode;

        protected override int MapForbidden(Forbidden forbidden) => ForbiddenExitCode;

        protected override int MapForbiddenFailureCase(IForbiddenFailureCase forbiddenFailureCase) => ForbiddenExitCode;

        protected override int MapConflict(Conflict conflict) => ConflictExitCode;

        protected override int MapValidationErrors(ValidationErrors validationErrors) => ValidationExitCode;

        protected override int MapValidationFailureCase(IValidationFailureCase validationFailureCase) => ValidationExitCode;

        protected override int MapCancelled(Cancelled cancelled) => UnknownExitCode;

        protected override int MapTimeout(Timeout timeout) => UnknownExitCode;

        protected override int MapTimeoutFailureCase(ITimeoutFailureCase timeoutFailureCase) => UnknownExitCode;

        protected override int MapRateLimited(RateLimited rateLimited) => UnknownExitCode;

        protected override int MapRateLimitedFailureCase(IRateLimitedFailureCase rateLimitedFailureCase) => UnknownExitCode;

        protected override int MapFailures(Failures failures) => UnknownExitCode;

        protected override int MapFailure(Failure failure) => UnknownExitCode;

        protected override int MapUnrecognized<TFailure>(TFailure failure) => UnknownExitCode;
    }

    /// <summary>Proves a case handled by <see cref="MapUnrecognized{TFailure}"/> can be overridden one level further.</summary>
    private sealed class RateLimitAwareExitCodeOutcomeMapper : ExitCodeOutcomeMapper
    {
        protected override int MapUnrecognized<TFailure>(TFailure failure) =>
            failure is StubCustomFailure ? 42 : base.MapUnrecognized(failure);
    }
}
