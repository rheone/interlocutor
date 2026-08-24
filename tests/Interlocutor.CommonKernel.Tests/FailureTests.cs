namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, equality, and default-value behavior of <see cref="Failure"/> and
/// <see cref="Failure{TErrorCode}"/>.
/// </summary>
public class FailureTests
{
    [Fact]
    public void Failure_DefaultValue_ReasonIsNull()
    {
        // Arrange & Act
        var defaultValue = default(Failure);

        // Assert
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void Failure_ConstructedWithNamedDefault_ReasonIsNull()
    {
        // Arrange & Act
        var failure = new Failure(Reason: null);

        // Assert
        Assert.Null(failure.Reason);
    }

    [Fact]
    public void Failure_WithReason_ReasonIsSet()
    {
        // Arrange & Act
        var failure = new Failure("Operation failed");

        // Assert
        Assert.Equal("Operation failed", failure.Reason);
    }

    [Fact]
    public void Failure_SameReason_AreEqual()
    {
        // Arrange
        var first = new Failure("Timeout");
        var second = new Failure("Timeout");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Failure_DifferentReason_AreNotEqual()
    {
        // Arrange
        var first = new Failure("Timeout");
        var second = new Failure("Disk full");

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Failure_DefaultValue_EqualsExplicitNullReason()
    {
        // Arrange
        var defaultValue = default(Failure);
        var explicitValue = new Failure(null);

        // Act & Assert
        Assert.Equal(defaultValue, explicitValue);
    }

    [Fact]
    public void FailureOfTErrorCode_DefaultValue_ErrorCodeIsDefault()
    {
        // Arrange & Act
        var defaultValue = default(Failure<int>);

        // Assert
        Assert.Equal(0, defaultValue.ErrorCode);
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_WithErrorCodeOnly_ReasonIsNull()
    {
        // Arrange & Act
        var failure = new Failure<int>(404);

        // Assert
        Assert.Equal(404, failure.ErrorCode);
        Assert.Null(failure.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_WithErrorCodeAndReason_BothAreSet()
    {
        // Arrange & Act
        var failure = new Failure<string>("payment_declined", "The card was declined");

        // Assert
        Assert.Equal("payment_declined", failure.ErrorCode);
        Assert.Equal("The card was declined", failure.Reason);
    }

    [Fact]
    public void FailureOfTErrorCode_SameErrorCodeAndReason_AreEqual()
    {
        // Arrange
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1001, "Email is required");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void FailureOfTErrorCode_DifferentErrorCode_AreNotEqual()
    {
        // Arrange
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1002, "Email is required");

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void FailureOfTErrorCode_DifferentReason_AreNotEqual()
    {
        // Arrange
        var first = new Failure<int>(1001, "Email is required");
        var second = new Failure<int>(1001, "Email is missing");

        // Act & Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Failure_WithNoReason_ReturnsBareTypeName()
    {
        // Arrange
        var failure = default(Failure);

        // Act
        var result = failure.ToString();

        // Assert
        Assert.Equal("Failure", result);
    }

    [Fact]
    public void Failure_WithReason_IncludesReason()
    {
        // Arrange
        var failure = new Failure("Operation failed");

        // Act
        var result = failure.ToString();

        // Assert
        Assert.Equal("Failure: Operation failed", result);
    }

    [Fact]
    public void FailureOfTErrorCode_WithNoReason_IncludesErrorCodeOnly()
    {
        // Arrange
        var failure = new Failure<int>(404);

        // Act
        var result = failure.ToString();

        // Assert
        Assert.Equal("Failure: 404", result);
    }

    [Fact]
    public void FailureOfTErrorCode_WithReason_IncludesErrorCodeAndReason()
    {
        // Arrange
        var failure = new Failure<string>("payment_declined", "The card was declined");

        // Act
        var result = failure.ToString();

        // Assert
        Assert.Equal("Failure: payment_declined - The card was declined", result);
    }
}
