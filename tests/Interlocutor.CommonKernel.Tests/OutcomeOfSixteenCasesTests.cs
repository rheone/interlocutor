namespace Interlocutor.CommonKernel.Tests;

using SixteenCaseOutcome = Interlocutor.CommonKernel.Outcome<
    string,
    int,
    bool,
    double,
    long,
    short,
    byte,
    char,
    float,
    decimal,
    System.DateTime,
    System.Guid,
    System.TimeSpan,
    uint,
    ulong,
    sbyte
>;

/// <summary>
/// Covers the sixteen-case generic <see cref="Outcome{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13,
/// T14, T15, T16}"/> union - the shipped family's actual ceiling, matching the BCL's widest
/// <see cref="Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult}"/> overload.
/// Unlike the per-arity suites for the low-arity, everyday shapes (two through six cases), this suite doesn't
/// exercise every one of the sixteen cases individually - it proves construction, <c>Match</c>, <c>Switch</c>,
/// <c>TryGetValue</c>, equality, and <c>ToString</c> all still work correctly at the far end of the supported
/// range, using the first, a middle, and the last case as representative positions.
/// </summary>
public class OutcomeOfSixteenCasesTests
{
    private static readonly Guid SampleGuid = Guid.Parse("8f14e45f-ceea-467e-bd0d-9d5f1b1e4d3c");
    private static readonly DateTime SampleDateTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan SampleTimeSpan = TimeSpan.FromMinutes(5);

    [Fact]
    public void Construct_FromFirstCase_TryGetValueSucceeds()
    {
        // Arrange & Act
        SixteenCaseOutcome result = "hello";

        // Assert
        Assert.True(result.TryGetValue(out string hit));
        Assert.Equal("hello", hit);
    }

    [Fact]
    public void Construct_FromMiddleCase_TryGetValueSucceeds()
    {
        // Arrange & Act
        var result = new SixteenCaseOutcome('x');

        // Assert
        Assert.True(result.TryGetValue(out char hit));
        Assert.Equal('x', hit);
        Assert.False(result.TryGetValue(out string miss));
        Assert.Null(miss);
    }

    [Fact]
    public void Construct_FromLastCase_TryGetValueSucceeds()
    {
        // Arrange & Act
        SixteenCaseOutcome result = (sbyte)13;

        // Assert
        Assert.True(result.TryGetValue(out sbyte hit));
        Assert.Equal((sbyte)13, hit);
    }

    [Fact]
    public void ImplicitConversion_FromMiddleCaseValue_ProducesThatCaseOutcome()
    {
        // Arrange & Act
        SixteenCaseOutcome result = decimal.One;

        // Assert
        Assert.True(result.TryGetValue(out decimal hit));
        Assert.Equal(decimal.One, hit);
    }

    [Fact]
    public void Match_OverEachCase_InvokesMatchingDelegate()
    {
        // Arrange
        SixteenCaseOutcome case1 = "hello";
        SixteenCaseOutcome case2 = 42;
        SixteenCaseOutcome case3 = true;
        SixteenCaseOutcome case4 = 3.14;
        SixteenCaseOutcome case5 = 100L;
        SixteenCaseOutcome case6 = (short)7;
        SixteenCaseOutcome case7 = (byte)8;
        SixteenCaseOutcome case8 = 'x';
        SixteenCaseOutcome case9 = 1.5f;
        SixteenCaseOutcome case10 = 9.99m;
        SixteenCaseOutcome case11 = SampleDateTime;
        SixteenCaseOutcome case12 = SampleGuid;
        SixteenCaseOutcome case13 = SampleTimeSpan;
        SixteenCaseOutcome case14 = 11u;
        SixteenCaseOutcome case15 = 12ul;
        SixteenCaseOutcome case16 = (sbyte)13;

        static string Project(SixteenCaseOutcome outcome) =>
            outcome.Match(
                v => "case1:" + v,
                v => "case2:" + v,
                v => "case3:" + v,
                v => "case4:" + v,
                v => "case5:" + v,
                v => "case6:" + v,
                v => "case7:" + v,
                v => "case8:" + v,
                v => "case9:" + v,
                v => "case10:" + v,
                v => "case11:" + v.Ticks,
                v => "case12:" + v,
                v => "case13:" + v,
                v => "case14:" + v,
                v => "case15:" + v,
                v => "case16:" + v
            );

        // Act & Assert
        Assert.Equal("case1:hello", Project(case1));
        Assert.Equal("case2:42", Project(case2));
        Assert.Equal("case3:True", Project(case3));
        Assert.Equal("case4:3.14", Project(case4));
        Assert.Equal("case5:100", Project(case5));
        Assert.Equal("case6:7", Project(case6));
        Assert.Equal("case7:8", Project(case7));
        Assert.Equal("case8:x", Project(case8));
        Assert.Equal("case9:1.5", Project(case9));
        Assert.Equal("case10:9.99", Project(case10));
        Assert.Equal("case11:" + SampleDateTime.Ticks, Project(case11));
        Assert.Equal("case12:" + SampleGuid, Project(case12));
        Assert.Equal("case13:" + SampleTimeSpan, Project(case13));
        Assert.Equal("case14:11", Project(case14));
        Assert.Equal("case15:12", Project(case15));
        Assert.Equal("case16:13", Project(case16));
    }

    [Fact]
    public void Switch_OverFirstMiddleAndLastCase_DispatchesToMatchingAction()
    {
        // Arrange
        SixteenCaseOutcome first = "hello";
        SixteenCaseOutcome middle = 'x';
        SixteenCaseOutcome last = (sbyte)13;

        static string Observe(SixteenCaseOutcome outcome)
        {
            string result = "unset";
            outcome.Switch(
                v => result = "case1:" + v,
                v => result = "case2:" + v,
                v => result = "case3:" + v,
                v => result = "case4:" + v,
                v => result = "case5:" + v,
                v => result = "case6:" + v,
                v => result = "case7:" + v,
                v => result = "case8:" + v,
                v => result = "case9:" + v,
                v => result = "case10:" + v,
                v => result = "case11:" + v.Ticks,
                v => result = "case12:" + v,
                v => result = "case13:" + v,
                v => result = "case14:" + v,
                v => result = "case15:" + v,
                v => result = "case16:" + v
            );
            return result;
        }

        // Act & Assert
        Assert.Equal("case1:hello", Observe(first));
        Assert.Equal("case8:x", Observe(middle));
        Assert.Equal("case16:13", Observe(last));
    }

    [Fact]
    public void TryGetValue_FirstAndLastCase_HitAndMiss()
    {
        // Arrange
        SixteenCaseOutcome first = "hello";
        SixteenCaseOutcome last = (sbyte)13;

        // Act & Assert
        Assert.True(first.TryGetValue(out string firstHit));
        Assert.Equal("hello", firstHit);
        Assert.False(last.TryGetValue(out string firstMiss));
        Assert.Null(firstMiss);

        Assert.True(last.TryGetValue(out sbyte lastHit));
        Assert.Equal((sbyte)13, lastHit);
        Assert.False(first.TryGetValue(out sbyte lastMiss));
        Assert.Equal(default, lastMiss);
    }

    [Fact]
    public void Equals_SameCaseValue_AreEqual()
    {
        // Arrange
        SixteenCaseOutcome outcome = SampleGuid;
        SixteenCaseOutcome sameGuid = SampleGuid;

        // Act & Assert
        Assert.Equal(outcome, sameGuid);
        Assert.True(outcome == sameGuid);
        Assert.Equal(outcome.GetHashCode(), sameGuid.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentCaseValue_AreUnequal()
    {
        // Arrange
        SixteenCaseOutcome first = "hello";
        SixteenCaseOutcome last = (sbyte)13;

        // Act & Assert
        Assert.NotEqual(first, last);
        Assert.True(first != last);
    }

    [Fact]
    public void Equals_SamePositionDifferentValue_AreUnequal()
    {
        // Arrange
        SixteenCaseOutcome one = 11u;
        SixteenCaseOutcome two = 99u;

        // Act & Assert
        Assert.NotEqual(one, two);
    }

    [Fact]
    public void ToString_ForwardsToActiveCasesOwnToString()
    {
        // Arrange
        SixteenCaseOutcome first = "hello";
        SixteenCaseOutcome last = (sbyte)13;

        // Act & Assert
        Assert.Equal("hello", first.ToString());
        Assert.Equal(((sbyte)13).ToString(), last.ToString());
    }

    [Fact]
    public void Create_FromFirstCase_BehavesIdenticallyToImplicitConversion()
    {
        // Arrange & Act
        var viaFactory = SixteenCaseOutcome.Create("hello");
        SixteenCaseOutcome viaImplicitConversion = "hello";

        // Assert
        Assert.Equal(viaImplicitConversion, viaFactory);
        Assert.True(viaFactory.TryGetValue(out string hit));
        Assert.Equal("hello", hit);
    }

    [Fact]
    public void Create_FromLastCase_BehavesIdenticallyToThePositionalConstructor()
    {
        // Arrange & Act
        var viaFactory = SixteenCaseOutcome.Create((sbyte)13);
        var viaConstructor = new SixteenCaseOutcome((sbyte)13);

        // Assert
        Assert.Equal(viaConstructor, viaFactory);
        Assert.True(viaFactory.TryGetValue(out sbyte hit));
        Assert.Equal((sbyte)13, hit);
    }
}
