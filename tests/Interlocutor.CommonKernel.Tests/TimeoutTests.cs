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
        var defaultValue = default(Timeout);
        Assert.Null(defaultValue.Message);
        Assert.Equal(TimeSpan.Zero, defaultValue.Budget);
        Assert.Equal(TimeSpan.Zero, defaultValue.Elapsed);
    }

    [Fact]
    public void ConstructedWithoutMessage_MessageIsNull()
    {
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));

        Assert.Null(timeout.Message);
        Assert.Equal(TimeSpan.FromSeconds(5), timeout.Budget);
        Assert.Equal(TimeSpan.FromSeconds(7), timeout.Elapsed);
    }

    [Fact]
    public void ConstructedWithMessage_AllPropertiesAreSet()
    {
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Downstream call was slow");

        Assert.Equal(TimeSpan.FromSeconds(5), timeout.Budget);
        Assert.Equal(TimeSpan.FromSeconds(7), timeout.Elapsed);
        Assert.Equal("Downstream call was slow", timeout.Message);
    }

    [Fact]
    public void SameValues_AreEqual()
    {
        var first = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Slow");
        var second = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Slow");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void DifferentElapsed_AreNotEqual()
    {
        var first = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));
        var second = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8));

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ToString_WithoutMessage_IncludesBudgetAndElapsed()
    {
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));

        Assert.Equal($"Timeout: Budget={TimeSpan.FromSeconds(5)}, Elapsed={TimeSpan.FromSeconds(7)}", timeout.ToString());
    }

    [Fact]
    public void ToString_WithMessage_IncludesBudgetElapsedAndMessage()
    {
        var timeout = new Timeout(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Downstream call was slow");

        Assert.Equal(
            $"Timeout: Budget={TimeSpan.FromSeconds(5)}, Elapsed={TimeSpan.FromSeconds(7)}, Downstream call was slow",
            timeout.ToString()
        );
    }
}
