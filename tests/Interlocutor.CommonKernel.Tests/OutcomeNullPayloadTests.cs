namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers a success (or failure) case constructed with a legitimately <see langword="null"/> payload - a
/// nullable reference type, or a <see cref="Nullable{T}"/> with no value. Every generated member must tell
/// that state apart from an unset (<see langword="default"/>) union, even though both store a
/// <see langword="null"/> <c>Value</c> - see <c>.scratch/repo-review-2026-09-02/findings/01-outcome-null-payload-no-discriminator.md</c>.
/// </summary>
public class OutcomeNullPayloadTests
{
    [Fact]
    public void ImplicitConversion_FromNullSuccessValue_TryGetValueSucceedsWithNullValue()
    {
        // Arrange & Act
        Outcome<string?, NotFound> result = (string?)null;

        // Assert
        Assert.True(result.TryGetValue(out string? value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_SuccessCaseOnNullPayloadSuccess_ReturnsTrueWithNullValue()
    {
        // Arrange
        Outcome<string?, NotFound> result = (string?)null;

        // Act
        var found = result.TryGetValue(out string? value);

        // Assert
        Assert.True(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_FailureCaseOnNullPayloadSuccess_ReturnsFalse()
    {
        // Arrange
        Outcome<string?, NotFound> result = (string?)null;

        // Act
        var found = result.TryGetValue(out NotFound value);

        // Assert
        Assert.False(found);
        Assert.Equal(default, value);
    }

    [Fact]
    public void Match_OverNullPayloadSuccess_InvokesSuccessDelegate()
    {
        // Arrange
        Outcome<string?, NotFound> result = (string?)null;

        // Act
        var message = result.Match(success => "success:" + (success ?? "<null>"), notFound => "not-found:" + notFound.Message);

        // Assert
        Assert.Equal("success:<null>", message);
    }

    [Fact]
    public void Switch_OverNullPayloadSuccess_InvokesSuccessAction()
    {
        // Arrange
        Outcome<string?, NotFound> result = (string?)null;
        string? observed = null;

        // Act
        result.Switch(
            success => observed = "success:" + (success ?? "<null>"),
            notFound => observed = "not-found:" + notFound.Message
        );

        // Assert
        Assert.Equal("success:<null>", observed);
    }

    [Fact]
    public void ActiveCaseType_OnNullPayloadSuccess_ResolvesTheSuccessCaseType()
    {
        // Arrange
        Outcome<string?, NotFound> result = (string?)null;

        // Act & Assert - the case type, not "the outcome's own container type" (see finding 07).
        Assert.Equal(typeof(string), ((IOutcomeCase)result).ActiveCaseType);
    }

    [Fact]
    public void ImplicitConversion_FromNullableValueTypeFailureWithNoValue_IsFailure()
    {
        // Arrange & Act - Nullable<T> with HasValue == false boxes to a null Value, same ambiguity as a
        // nullable reference type. TSuccessResult (NotFound) never accepts null, so the null payload
        // unambiguously belongs to the failure case.
        Outcome<NotFound, int?> result = (int?)null;

        // Assert
        Assert.False(result.TryGetValue(out NotFound _));
        Assert.True(result.TryGetValue(out int? failureValue));
        Assert.Null(failureValue);
        Assert.Equal(typeof(int?), ((IOutcomeCase)result).ActiveCaseType);
    }

    [Fact]
    public void UnsetDefault_WhenNoCaseTypeAcceptsNull_IsNeitherCase()
    {
        // Arrange & Act - neither Success nor Failure can ever be null, so a genuinely unset union (never
        // assigned a case) is still distinguishable: ActiveCaseType stays null.
        var result = default(Outcome<Success, Failure>);

        // Assert
        Assert.False(result.TryGetValue(out Success _));
        Assert.False(result.TryGetValue(out Failure _));
        Assert.Null(((IOutcomeCase)result).ActiveCaseType);
        Assert.Throws<InvalidOperationException>(() => result.Match(_ => "success", _ => "failure"));
    }

    [Fact]
    public void Equals_TwoNullPayloadSuccesses_AreEqual()
    {
        // Arrange
        Outcome<string?, NotFound> first = (string?)null;
        Outcome<string?, NotFound> second = (string?)null;

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
