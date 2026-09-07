namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the six-case generic
/// <see cref="Outcome{T1, T2, T3, T4, T5, T6}"/> union: construction from
/// each case type, implicit conversion from a bare case value, exhaustive switch pattern matching, and the
/// generated <c>Match</c>/<c>Switch</c>/<c>TryGetValue</c>/<c>ToString</c> member set.
/// </summary>
public class OutcomeOfSixCasesTests
{
    [Fact]
    public void Construct_FromSuccess_TryGetValueSucceeds()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>("hello");

        // Assert
        Assert.True(result.TryGetValue(out string _));
    }

    [Fact]
    public void Construct_FromCase2_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(new NotFound("missing"));

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void Construct_FromCase3_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(new Forbidden("denied"));

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void Construct_FromCase4_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new Conflict("stale", "CONFLICT")
        );

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void Construct_FromCase5_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(
            new ValidationError("Name is required")
        );

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void Construct_FromCase6_TryGetValueFails()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure>(new Failure("boom"));

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        // Assert
        Assert.True(result.TryGetValue(out string _));
    }

    [Fact]
    public void ImplicitConversion_FromCase6Value_ProducesCase6Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

        // Assert
        Assert.False(result.TryGetValue(out string _));
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new NotFound("missing");

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Forbidden("denied");

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Conflict("stale", "CONFLICT");

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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new ValidationError(
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
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = new Failure("boom");

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
    public void Value_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = "hello";

        // Act & Assert
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Value_OverCase6_ReturnsUnderlyingValue()
    {
        // Arrange
        var failure = new Failure("boom");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> result = failure;

        // Act & Assert
        Assert.Equal(failure, result.Value);
    }

    [Fact]
    public void Match_OverEachCase_InvokesMatchingDelegate()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case3 = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case4 = new Conflict("stale", "CONFLICT");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case5 = new ValidationError(
            "Name is required"
        );
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case6 = new Failure("boom");

        static string Project(Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> outcome) =>
            outcome.Match(
                s => "success:" + s,
                notFound => "case2:" + notFound.Message,
                forbidden => "case3:" + forbidden.Reason,
                conflict => "case4:" + conflict.Message,
                validationError => "case5:" + validationError.Message,
                failure => "case6:" + failure.Reason
            );

        // Act & Assert
        Assert.Equal("success:hello", Project(success));
        Assert.Equal("case2:missing", Project(case2));
        Assert.Equal("case3:denied", Project(case3));
        Assert.Equal("case4:stale", Project(case4));
        Assert.Equal("case5:Name is required", Project(case5));
        Assert.Equal("case6:boom", Project(case6));
    }

    [Fact]
    public void Switch_GeneratedOverload_DispatchesToMatchingAction()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case6 = new Failure("boom");

        static string Observe(Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> outcome)
        {
            string result = "unset";
            outcome.Switch(
                s => result = "success:" + s,
                notFound => result = "case2:" + notFound.Message,
                forbidden => result = "case3:" + forbidden.Reason,
                conflict => result = "case4:" + conflict.Message,
                validationError => result = "case5:" + validationError.Message,
                failure => result = "case6:" + failure.Reason
            );
            return result;
        }

        // Act & Assert
        Assert.Equal("success:hello", Observe(success));
        Assert.Equal("case6:boom", Observe(case6));
    }

    [Fact]
    public void TryGetValue_Success_HitAndMiss()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> other = new NotFound("missing");

        // Act & Assert
        Assert.True(success.TryGetValue(out string hit));
        Assert.Equal("hello", hit);
        Assert.False(other.TryGetValue(out string miss));
        Assert.Null(miss);
    }

    [Fact]
    public void TryGetValue_Case2_HitAndMiss()
    {
        // Arrange
        var notFound = new NotFound("missing");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> hitOutcome = notFound;
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out NotFound hit));
        Assert.Equal(notFound, hit);
        Assert.False(missOutcome.TryGetValue(out NotFound miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case3_HitAndMiss()
    {
        // Arrange
        var forbidden = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> hitOutcome = forbidden;
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Forbidden hit));
        Assert.Equal(forbidden, hit);
        Assert.False(missOutcome.TryGetValue(out Forbidden miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case4_HitAndMiss()
    {
        // Arrange
        var conflict = new Conflict("stale", "CONFLICT");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> hitOutcome = conflict;
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Conflict hit));
        Assert.Equal(conflict, hit);
        Assert.False(missOutcome.TryGetValue(out Conflict miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case5_HitAndMiss()
    {
        // Arrange
        var validationError = new ValidationError("Name is required");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> hitOutcome = validationError;
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out ValidationError hit));
        Assert.Equal(validationError, hit);
        Assert.False(missOutcome.TryGetValue(out ValidationError miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void TryGetValue_Case6_HitAndMiss()
    {
        // Arrange
        var failure = new Failure("boom");
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> hitOutcome = failure;
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Failure hit));
        Assert.Equal(failure, hit);
        Assert.False(missOutcome.TryGetValue(out Failure miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void ToString_ForwardsToActiveCasesOwnToString()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> success = "hello";
        Outcome<string, NotFound, Forbidden, Conflict, ValidationError, Failure> case6 = new Failure("boom");

        // Act & Assert
        Assert.Equal("hello", success.ToString());
        Assert.Equal(new Failure("boom").ToString(), case6.ToString());
    }
}
