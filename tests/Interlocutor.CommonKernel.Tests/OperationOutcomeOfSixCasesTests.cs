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
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>("hello");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new NotFound("missing")
        );

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new Forbidden("denied")
        );

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase4_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new Conflict("stale", "CONFLICT")
        );

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase5_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new ValidationError("Name is required")
        );

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase6_IsSuccessFalse()
    {
        var result = new OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(new Failure("boom"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase6Value_ProducesCase6Outcome()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

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

        Assert.Equal("hello", message);
    }

    [Fact]
    public void Switch_OverCase2_MatchesCase2()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new NotFound("missing");

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

        Assert.Equal("missing", message);
    }

    [Fact]
    public void Switch_OverCase3_MatchesCase3()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Forbidden("denied");

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

        Assert.Equal("denied", message);
    }

    [Fact]
    public void Switch_OverCase4_MatchesCase4()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Conflict(
            "stale",
            "CONFLICT"
        );

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

        Assert.Equal("stale", message);
    }

    [Fact]
    public void Switch_OverCase5_MatchesCase5()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new ValidationError(
            "Name is required"
        );

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

        Assert.Equal("Name is required", message);
    }

    [Fact]
    public void Switch_OverCase6_MatchesCase6()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

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

        Assert.Equal("boom", message);
    }

    [Fact]
    public void GetValue_OverSuccess_ReturnsUnderlyingValue()
    {
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        Assert.Equal("hello", result.GetValue());
    }

    [Fact]
    public void GetValue_OverCase6_ReturnsUnderlyingValue()
    {
        var failure = new Failure("boom");
        OperationOutcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = failure;

        Assert.Equal(failure, result.GetValue());
    }
}
