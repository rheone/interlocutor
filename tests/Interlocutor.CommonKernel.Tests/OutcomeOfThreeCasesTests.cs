namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the three-case generic <see cref="Outcome{TSuccessResult, TCase2, TCase3}"/> union: construction
/// from each case type, implicit conversion from a bare case value, exhaustive switch pattern matching, and
/// the generated <c>Match</c>/<c>Switch</c>/<c>TryGetValue</c>/<c>ToString</c> member set.
/// </summary>
public class OutcomeOfThreeCasesTests
{
    [Fact]
    public void Construct_FromSuccess_IsSuccessTrue()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden>("hello");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase2_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden>(new NotFound("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Construct_FromCase3_IsSuccessFalse()
    {
        // Arrange & Act
        var result = new Outcome<string, NotFound, Forbidden>(new Forbidden("denied"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromSuccessValue_ProducesSuccessOutcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden> result = "hello";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase2Value_ProducesCase2Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden> result = new NotFound("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ImplicitConversion_FromCase3Value_ProducesCase3Outcome()
    {
        // Arrange & Act
        Outcome<string, NotFound, Forbidden> result = new Forbidden("denied");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Switch_OverSuccess_MatchesSuccessCase()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> result = "hello";

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
        Outcome<string, NotFound, Forbidden> result = new NotFound("missing");

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
        Outcome<string, NotFound, Forbidden> result = new Forbidden("denied");

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
    public void Value_OverSuccess_ReturnsUnderlyingValue()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> result = "hello";

        // Act & Assert
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Value_OverCase2_ReturnsUnderlyingValue()
    {
        // Arrange
        var notFound = new NotFound("missing");
        Outcome<string, NotFound, Forbidden> result = notFound;

        // Act & Assert
        Assert.Equal(notFound, result.Value);
    }

    [Fact]
    public void Value_OverCase3_ReturnsUnderlyingValue()
    {
        // Arrange
        var forbidden = new Forbidden("denied");
        Outcome<string, NotFound, Forbidden> result = forbidden;

        // Act & Assert
        Assert.Equal(forbidden, result.Value);
    }

    [Fact]
    public void Match_OverEachCase_InvokesMatchingDelegate()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> success = "hello";
        Outcome<string, NotFound, Forbidden> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden> case3 = new Forbidden("denied");

        static string Project(Outcome<string, NotFound, Forbidden> outcome) =>
            outcome.Match(
                s => "success:" + s,
                notFound => "case2:" + notFound.Message,
                forbidden => "case3:" + forbidden.Reason
            );

        // Act & Assert
        Assert.Equal("success:hello", Project(success));
        Assert.Equal("case2:missing", Project(case2));
        Assert.Equal("case3:denied", Project(case3));
    }

    [Fact]
    public void Switch_GeneratedOverload_DispatchesToMatchingAction()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> success = "hello";
        Outcome<string, NotFound, Forbidden> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden> case3 = new Forbidden("denied");

        static string Observe(Outcome<string, NotFound, Forbidden> outcome)
        {
            string result = "unset";
            outcome.Switch(
                s => result = "success:" + s,
                notFound => result = "case2:" + notFound.Message,
                forbidden => result = "case3:" + forbidden.Reason
            );
            return result;
        }

        // Act & Assert
        Assert.Equal("success:hello", Observe(success));
        Assert.Equal("case2:missing", Observe(case2));
        Assert.Equal("case3:denied", Observe(case3));
    }

    [Fact]
    public void TryGetValue_Success_HitAndMiss()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> success = "hello";
        Outcome<string, NotFound, Forbidden> other = new NotFound("missing");

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
        Outcome<string, NotFound, Forbidden> hitOutcome = notFound;
        Outcome<string, NotFound, Forbidden> missOutcome = "hello";

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
        Outcome<string, NotFound, Forbidden> hitOutcome = forbidden;
        Outcome<string, NotFound, Forbidden> missOutcome = "hello";

        // Act & Assert
        Assert.True(hitOutcome.TryGetValue(out Forbidden hit));
        Assert.Equal(forbidden, hit);
        Assert.False(missOutcome.TryGetValue(out Forbidden miss));
        Assert.Equal(default, miss);
    }

    [Fact]
    public void ToString_ForwardsToActiveCasesOwnToString()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> success = "hello";
        Outcome<string, NotFound, Forbidden> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden> case3 = new Forbidden("denied");

        // Act & Assert
        Assert.Equal("hello", success.ToString());
        Assert.Equal(new NotFound("missing").ToString(), case2.ToString());
        Assert.Equal(new Forbidden("denied").ToString(), case3.ToString());
    }
}
