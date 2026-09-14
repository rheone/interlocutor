namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;

/// <summary>Tests for <see cref="RolloutGate"/>.</summary>
public class RolloutGateTests
{
    [Fact]
    public void ShouldRun_ZeroPercent_NeverRuns()
    {
        // Arrange
        var options = new RolloutOptions { PercentageEnabled = 0 };

        // Act & Assert - a large sample size makes a flaky true-by-chance result effectively impossible.
        for (var i = 0; i < 1_000; i++)
        {
            Assert.False(RolloutGate.ShouldRun(options));
        }
    }

    [Fact]
    public void ShouldRun_HundredPercent_AlwaysRuns()
    {
        // Arrange
        var options = new RolloutOptions { PercentageEnabled = 100 };

        // Act & Assert
        for (var i = 0; i < 1_000; i++)
        {
            Assert.True(RolloutGate.ShouldRun(options));
        }
    }

    [Fact]
    public void ShouldRun_FiftyPercent_RunsApproximatelyHalfTheTime()
    {
        // Arrange - a statistical test with tolerance, not an exact count: the draw is genuinely random, so
        // only the aggregate frequency over a large sample is asserted.
        var options = new RolloutOptions { PercentageEnabled = 50 };
        const int trials = 20_000;

        // Act
        var runCount = 0;
        for (var i = 0; i < trials; i++)
        {
            if (RolloutGate.ShouldRun(options))
            {
                runCount++;
            }
        }

        // Assert - +/- 5 percentage points around the expected 50% is generous enough to make this flake
        // only astronomically rarely, while still catching a badly wrong implementation.
        var ratio = (double)runCount / trials;
        Assert.InRange(ratio, 0.45, 0.55);
    }

    [Fact]
    public void ShouldRun_TenPercent_RunsApproximatelyOneTenthOfTheTime()
    {
        // Arrange
        var options = new RolloutOptions { PercentageEnabled = 10 };
        const int trials = 20_000;

        // Act
        var runCount = 0;
        for (var i = 0; i < trials; i++)
        {
            if (RolloutGate.ShouldRun(options))
            {
                runCount++;
            }
        }

        // Assert
        var ratio = (double)runCount / trials;
        Assert.InRange(ratio, 0.07, 0.13);
    }

    [Fact]
    public void ShouldRun_NullOptions_Throws() => Assert.Throws<ArgumentNullException>(() => RolloutGate.ShouldRun(null!));
}
