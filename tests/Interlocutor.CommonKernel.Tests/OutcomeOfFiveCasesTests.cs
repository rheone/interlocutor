namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the five-case generic <see cref="Outcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5}"/>
/// union: construction from each case type, implicit conversion from a bare case value, and exhaustive switch
/// pattern matching.
/// </summary>
public class OutcomeOfFiveCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError>(new Forbidden("denied"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError>(new Conflict("stale", "CONFLICT"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase5_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError>(
            new ValidationError("Name is required")
        );

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase5Value_ProducesCase5Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = new ValidationError("Name is required");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError => "invalid",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError => "invalid",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = new Forbidden("denied");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            Conflict => "conflict",
            ValidationError => "invalid",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = new Conflict("stale", "CONFLICT");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict conflict => conflict.Message,
            ValidationError => "invalid",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("stale", message);
    }

    [Fact]
    public void Switch_OverCase5_MatchesCase5()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = new ValidationError("Name is required");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError validationError => validationError.Message,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("Name is required", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = "hello";

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public void GetValue_OverCase5_ReturnsUnderlyingValue()
    {
        // Arrange
        var validationError = new ValidationError("Name is required");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError> result = validationError;

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal(validationError, value);
    }
}
