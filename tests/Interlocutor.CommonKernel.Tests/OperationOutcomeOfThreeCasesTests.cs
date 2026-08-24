namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the three-case generic <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3}"/> union: construction
/// from each case type, implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeOfThreeCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden>(new Forbidden("denied"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden> result = new NotFound("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden> result = new Forbidden("denied");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden> result = new Forbidden("denied");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("denied", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden> result = "hello";

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public void GetValue_OverCase2_ReturnsUnderlyingValue()
    {
        // Arrange
        var notFound = new NotFound("missing");
        OperationOutcome<string, NotFound, Forbidden> result = notFound;

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal(notFound, value);
    }

    [Fact]
    public void GetValue_OverCase3_ReturnsUnderlyingValue()
    {
        // Arrange
        var forbidden = new Forbidden("denied");
        OperationOutcome<string, NotFound, Forbidden> result = forbidden;

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal(forbidden, value);
    }
}
