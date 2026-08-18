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
        var rateLimited = new RateLimited("per-user");

        Assert.Equal("per-user", rateLimited.LimitKey);
        Assert.Null(rateLimited.RetryAfter);
        Assert.Null(rateLimited.Message);
    }

    [Fact]
    public void ConstructedWithAllValues_AllPropertiesAreSet()
    {
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        Assert.Equal("per-user", rateLimited.LimitKey);
        Assert.Equal(TimeSpan.FromSeconds(30), rateLimited.RetryAfter);
        Assert.Equal("Too many requests", rateLimited.Message);
    }

    [Fact]
    public void SameValues_AreEqual()
    {
        var first = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");
        var second = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentLimitKey_AreNotEqual()
    {
        var first = new RateLimited("per-user");
        var second = new RateLimited("per-tenant");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutMessage_IncludesLimitKey()
    {
        var rateLimited = new RateLimited("per-user");
        Assert.Equal("RateLimited: per-user", rateLimited.ToString());
    }

    [Fact]
    public void ToString_WithMessage_IncludesLimitKeyAndMessage()
    {
        var rateLimited = new RateLimited("per-user", TimeSpan.FromSeconds(30), "Too many requests");
        Assert.Equal("RateLimited: per-user, Too many requests", rateLimited.ToString());
    }
}
