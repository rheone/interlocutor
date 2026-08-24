namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, default-value behavior, and <see cref="Timeout.ToString"/> of <see cref="Timeout"/>.
/// </summary>
public class TimeoutTests
{
    [Fact]
    public void DefaultValue_MessageIsNull()
    {
        // Arrange & Act
        var defaultValue = default(Timeout);

        // Assert
        Assert.Null(defaultValue.Message);
        Assert.Equal(TimeSpan.Zero, defaultValue.Budget);
        Assert.Equal(TimeSpan.Zero, defaultValue.Elapsed);
    }

    [Fact]
    public void ConstructedWithoutMessage_MessageIsNull()
    {
        // Arrange & Act
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));

        // Assert
        Assert.Null(timeout.Message);
        Assert.Equal(TimeSpan.FromSeconds(5), timeout.Budget);
        Assert.Equal(TimeSpan.FromSeconds(7), timeout.Elapsed);
    }

    [Fact]
    public void ConstructedWithMessage_AllPropertiesAreSet()
    {
        // Arrange & Act
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Downstream call was slow");

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), timeout.Budget);
        Assert.Equal(TimeSpan.FromSeconds(7), timeout.Elapsed);
        Assert.Equal("Downstream call was slow", timeout.Message);
    }

    [Fact]
    public void SameValues_AreEqual()
    {
        // Arrange
        var first = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Slow");
        var second = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Slow");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentElapsed_AreNotEqual()
    {
        // Arrange
        var first = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));
        var second = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8));

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutMessage_IncludesBudgetAndElapsed()
    {
        // Arrange
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));

        // Act
        var result = timeout.ToString();

        // Assert
        Assert.Equal($"Timeout: Budget={TimeSpan.FromSeconds(5)}, Elapsed={TimeSpan.FromSeconds(7)}", result);
    }

    [Fact]
    public void ToString_WithMessage_IncludesBudgetElapsedAndMessage()
    {
        // Arrange
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Downstream call was slow");

        // Act
        var result = timeout.ToString();

        // Assert
        Assert.Equal(
            $"Timeout: Budget={TimeSpan.FromSeconds(5)}, Elapsed={TimeSpan.FromSeconds(7)}, Downstream call was slow",
            result
        );
    }
}
