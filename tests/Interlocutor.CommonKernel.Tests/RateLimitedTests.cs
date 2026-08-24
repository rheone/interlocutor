namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Tests construction, default-value behavior, and <see cref="RateLimited.ToString"/> of <see cref="RateLimited"/>.
/// </summary>
public class RateLimitedTests
{
    [Fact]
    public void ConstructedWithLimitKeyOnly_OptionalsAreNull()
    {
        // Arrange & Act
        var rateLimited = new RateLimited("per-user");

        // Assert
        Assert.Equal("per-user", rateLimited.LimitKey);
        Assert.Null(rateLimited.RetryAfter);
        Assert.Null(rateLimited.Message);
    }

    [Fact]
    public void ConstructedWithAllValues_AllPropertiesAreSet()
    {
        // Arrange & Act
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        // Assert
        Assert.Equal("per-user", rateLimited.LimitKey);
        Assert.Equal(TimeSpan.FromSeconds(30), rateLimited.RetryAfter);
        Assert.Equal("Too many requests", rateLimited.Message);
    }

    [Fact]
    public void SameValues_AreEqual()
    {
        // Arrange
        var first = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");
        var second = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentLimitKey_AreNotEqual()
    {
        // Arrange
        var first = new RateLimited("per-user");
        var second = new RateLimited("per-tenant");

        // Act & Assert
        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutMessage_IncludesLimitKey()
    {
        // Arrange
        var rateLimited = new RateLimited("per-user");

        // Act
        var result = rateLimited.ToString();

        // Assert
        Assert.Equal("RateLimited: per-user", result);
    }

    [Fact]
    public void ToString_WithMessage_IncludesLimitKeyAndMessage()
    {
        // Arrange
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        // Act
        var result = rateLimited.ToString();

        // Assert
        Assert.Equal("RateLimited: per-user, Too many requests", result);
    }
}
