namespace Interlocutor.FluentValidation.Tests;

using global::FluentValidation;
using global::FluentValidation.Results;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, default-value behavior, fidelity, and <see cref="FluentValidationErrors.ToString"/> of
/// <see cref="FluentValidationErrors"/>.
/// </summary>
public class FluentValidationErrorsTests
{
    [Fact]
    public void Constructor_NullFailures_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FluentValidationErrors(null!));
    }

    [Fact]
    public void Constructor_EmptyFailures_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FluentValidationErrors([]));
    }

    [Fact]
    public void Constructor_PreservesEveryFieldOfEveryFailure()
    {
        // Arrange
        var customState = new object();
        var failure = new ValidationFailure("Email", "'Email' is not a valid email address.")
        {
            ErrorCode = "EmailValidator",
            Severity = Severity.Warning,
            CustomState = customState,
            AttemptedValue = "not-an-email",
        };

        // Act
        var errors = new FluentValidationErrors([failure]);

        // Assert - every field FluentValidation attaches to a ValidationFailure round-trips unchanged.
        var roundTripped = Assert.Single(errors.Failures);
        Assert.Equal("Email", roundTripped.PropertyName);
        Assert.Equal("'Email' is not a valid email address.", roundTripped.ErrorMessage);
        Assert.Equal("EmailValidator", roundTripped.ErrorCode);
        Assert.Equal(Severity.Warning, roundTripped.Severity);
        Assert.Same(customState, roundTripped.CustomState);
        Assert.Equal("not-an-email", roundTripped.AttemptedValue);
    }

    [Fact]
    public void Messages_ProjectsErrorMessageFromEveryFailure()
    {
        // Arrange
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Age", "Age must be non-negative"),
        };

        // Act
        var errors = new FluentValidationErrors(failures);

        // Assert
        Assert.Equal(["Name is required", "Age must be non-negative"], errors.Messages);
    }

    [Fact]
    public void Messages_DoesNotDeduplicate()
    {
        // Arrange - unlike ValidationErrors, FluentValidationErrors wraps every ValidationFailure verbatim,
        // even when two failures happen to carry the same message.
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Name", "Name is required"),
        };

        // Act
        var errors = new FluentValidationErrors(failures);

        // Assert
        Assert.Equal(2, errors.Failures.Count);
        Assert.Equal(["Name is required", "Name is required"], errors.Messages);
    }

    [Fact]
    public void DefaultValue_FailuresIsEmpty()
    {
        // Arrange & Act
        var defaultValue = default(FluentValidationErrors);

        // Assert
        Assert.Empty(defaultValue.Failures);
        Assert.Empty(defaultValue.Messages);
    }

    [Fact]
    public void SatisfiesIValidationFailureCase()
    {
        // Arrange
        var errors = new FluentValidationErrors([new ValidationFailure("Name", "Name is required")]);

        // Act
        IValidationFailureCase failureCase = errors;

        // Assert
        Assert.Equal(errors.Messages, failureCase.Messages);
    }

    [Fact]
    public void ToString_DefaultValue_ReturnsTypeNameOnly()
    {
        // Arrange & Act
        var result = default(FluentValidationErrors).ToString();

        // Assert
        Assert.Equal("FluentValidationErrors", result);
    }

    [Fact]
    public void ToString_WithFailures_IncludesMessages()
    {
        // Arrange
        var errors = new FluentValidationErrors([
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Age", "Age must be non-negative"),
        ]);

        // Act
        var result = errors.ToString();

        // Assert
        Assert.Equal("FluentValidationErrors: Name is required, Age must be non-negative", result);
    }
}
