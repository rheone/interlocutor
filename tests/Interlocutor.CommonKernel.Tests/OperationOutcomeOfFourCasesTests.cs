namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the four-case generic <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4}"/> union:
/// construction from each case type, implicit conversion from a bare case value, and exhaustive switch pattern
/// matching.
/// </summary>
public class OperationOutcomeOfFourCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new Forbidden("denied"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict>(new Conflict("stale", "CONFLICT"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase4Value_ProducesCase4Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Forbidden("denied");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            Conflict => "conflict",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = new Conflict("stale", "CONFLICT");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict conflict => conflict.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("stale", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = "hello";

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public void GetValue_OverCase4_ReturnsUnderlyingValue()
    {
        // Arrange
        var conflict = new Conflict("stale", "CONFLICT");
        OperationOutcome<string, NotFound, Forbidden, Conflict> result = conflict;

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal(conflict, value);
    }
}
