namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the non-generic, two-case <see cref="Outcome"/> union: construction from each case type,
/// implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OutcomeTests
{
    [Fact]
    public void Construct_FromSuccess_TryGetValueSucceeds()
    {
        // Arrange & Act
        var result = new Outcome(default(Success));

        // Assert
        Assert.True(result.TryGetValue(out Success _));
    }

    [Fact]
    public void Construct_FromFailure_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome(new Failure("boom"));

        // Assert
        Assert.False(result.TryGetValue(out Success _));
    }

    [Fact]
    public void ImplicitConversion_FromSuccess_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome result = default(Success);

        // Assert
        Assert.True(result.TryGetValue(out Success _));
    }

    [Fact]
    public void ImplicitConversion_FromFailure_ProducesFailureOutcome()
    {
        // Arrange & Act
        Outcome result = new Failure("boom");

        // Assert
        Assert.False(result.TryGetValue(out Success _));
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome result = default(Success);

        // Act
        var message = result.Value switch
        {
            Success => "success",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("success", message);
    }

    [Fact]
    public void Switch_OverFailure_MatchesFailureCase()
    {
        // Arrange
        Outcome result = new Failure("boom");

        // Act
        var message = result.Value switch
        {
            Success => "success",
            Failure failure => failure.Reason,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("boom", message);
    }
}
