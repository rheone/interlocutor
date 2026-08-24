namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the six-case generic
/// <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6}"/> union: construction from
/// each case type, implicit conversion from a bare case value, and exhaustive switch pattern matching.
/// </summary>
public class OperationOutcomeOfSixCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new NotFound("missing")
        );

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new Forbidden("denied")
        );

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new Conflict("stale", "CONFLICT")
        );

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase5_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new ValidationError("Name is required")
        );

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase6_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(new Failure("boom"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase6Value_ProducesCase6Outcome()
    {
        // Arrange & Act
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError => "invalid",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new NotFound("missing");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound notFound => notFound.Message,
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError => "invalid",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Forbidden("denied");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden forbidden => forbidden.Reason,
            Conflict => "conflict",
            ValidationError => "invalid",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Conflict(
            "stale",
            "CONFLICT"
        );

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict conflict => conflict.Message,
            ValidationError => "invalid",
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("stale", message);
    }

    [Fact]
    public void Switch_OverCase5_MatchesCase5()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new ValidationError(
            "Name is required"
        );

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError validationError => validationError.Message,
            Failure => "failure",
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("Name is required", message);
    }

    [Fact]
    public void Switch_OverCase6_MatchesCase6()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

        // Act
        var message = result.Value switch
        {
            string success => success,
            NotFound => "not found",
            Forbidden => "forbidden",
            Conflict => "conflict",
            ValidationError => "invalid",
            Failure failure => failure.Reason,
            _ => throw new InvalidOperationException("Unreachable"),
        };

        // Assert
        Assert.Equal("boom", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public void GetValue_OverCase6_ReturnsUnderlyingValue()
    {
        // Arrange
        var failure = new Failure("boom");
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = failure;

        // Act
        var value = result.GetValue();

        // Assert
        Assert.Equal(failure, value);
    }
}
