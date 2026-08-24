namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the non-generic, two-case <see cref="OperationOutcome"/> union: construction from each case type,
/// implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new OperationOutcome(default(Success));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromFailure_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome(new Failure("boom"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccess_ProducesSuccessOutcome()
    {
        // Arrange & Act
        OperationOutcome result = default(Success);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromFailure_ProducesFailureOutcome()
    {
        // Arrange & Act
        OperationOutcome result = new Failure("boom");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        OperationOutcome result = default(Success);

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
        OperationOutcome result = new Failure("boom");

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
