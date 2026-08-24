namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, default-value behavior, and <see cref="Cancelled.ToString"/> of <see cref="Cancelled"/>.
/// </summary>
public class CancelledTests
{
    [Fact]
    public void DefaultValue_ReasonIsNull()
    {
        // Arrange & Act
        var defaultValue = default(Cancelled);

        // Assert
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void ConstructedWithNamedDefault_ReasonIsNull()
    {
        // Arrange & Act
        var cancelled = new Cancelled(Reason: null);

        // Assert
        Assert.Null(cancelled.Reason);
    }

    [Fact]
    public void WithReason_ReasonIsSet()
    {
        // Arrange & Act
        var cancelled = new Cancelled("User cancelled the request");

        // Assert
        Assert.Equal("User cancelled the request", cancelled.Reason);
    }

    [Fact]
    public void SameReason_AreEqual()
    {
        // Arrange
        var first = new Cancelled("Shutting down");
        var second = new Cancelled("Shutting down");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentReason_AreNotEqual()
    {
        // Arrange
        var first = new Cancelled("Shutting down");
        var second = new Cancelled("Deadline exceeded");

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutReason_ReturnsBareTypeName()
    {
        // Arrange
        var cancelled = default(Cancelled);

        // Act
        var result = cancelled.ToString();

        // Assert
        Assert.Equal("Cancelled", result);
    }

    [Fact]
    public void ToString_WithReason_IncludesReason()
    {
        // Arrange
        var cancelled = new Cancelled("User cancelled the request");

        // Act
        var result = cancelled.ToString();

        // Assert
        Assert.Equal("Cancelled: User cancelled the request", result);
    }
}
