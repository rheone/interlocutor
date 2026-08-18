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
        var defaultValue = default(Cancelled);
        Assert.Null(defaultValue.Reason);
    }

    [Fact]
    public void ConstructedWithNamedDefault_ReasonIsNull()
    {
        var cancelled = new Cancelled(Reason: null);
        Assert.Null(cancelled.Reason);
    }

    [Fact]
    public void WithReason_ReasonIsSet()
    {
        var cancelled = new Cancelled("User cancelled the request");
        Assert.Equal("User cancelled the request", cancelled.Reason);
    }

    [Fact]
    public void SameReason_AreEqual()
    {
        var first = new Cancelled("Shutting down");
        var second = new Cancelled("Shutting down");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentReason_AreNotEqual()
    {
        var first = new Cancelled("Shutting down");
        var second = new Cancelled("Deadline exceeded");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutReason_ReturnsBareTypeName()
    {
        var cancelled = default(Cancelled);
        Assert.Equal("Cancelled", cancelled.ToString());
    }

    [Fact]
    public void ToString_WithReason_IncludesReason()
    {
        var cancelled = new Cancelled("User cancelled the request");
        Assert.Equal("Cancelled: User cancelled the request", cancelled.ToString());
    }
}
