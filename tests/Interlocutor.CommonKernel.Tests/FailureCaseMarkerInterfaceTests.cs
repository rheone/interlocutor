namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Confirms each built-in failure case with exactly one shipped implementation today
/// (<see cref="ValidationErrors"/>, <see cref="RateLimited"/>, <see cref="Timeout"/>, <see cref="Forbidden"/>)
/// satisfies its corresponding marker interface with no observable behavior change - the interface member
/// always reflects the same value as the struct's own existing property.
/// </summary>
public class FailureCaseMarkerInterfaceTests
{
    [Fact]
    public void ValidationErrors_SatisfiesIValidationFailureCase()
    {
        // Arrange
        var validationErrors = new ValidationErrors(["Name is required", "Email is required"]);

        // Act
        IValidationFailureCase failureCase = validationErrors;

        // Assert
        Assert.Equal(validationErrors.Messages, failureCase.Messages);
    }

    [Fact]
    public void RateLimited_SatisfiesIRateLimitedFailureCase()
    {
        // Arrange
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        // Act
        IRateLimitedFailureCase failureCase = rateLimited;

        // Assert
        Assert.Equal(rateLimited.Message, failureCase.Message);
    }

    [Fact]
    public void Timeout_SatisfiesITimeoutFailureCase()
    {
        // Arrange
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Downstream call was slow");

        // Act
        ITimeoutFailureCase failureCase = timeout;

        // Assert
        Assert.Equal(timeout.Message, failureCase.Message);
    }

    [Fact]
    public void Forbidden_SatisfiesIForbiddenFailureCase()
    {
        // Arrange
        var forbidden = new Forbidden("Missing required role");

        // Act
        IForbiddenFailureCase failureCase = forbidden;

        // Assert
        Assert.Equal(forbidden.Reason, failureCase.Reason);
    }
}
