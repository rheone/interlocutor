namespace Interlocutor.CommonKernel.Tests;

using System.Runtime.CompilerServices;

/// <summary>
/// Covers the single-case <see cref="Outcome{T}"/>: construction and the always-succeeding
/// <c>TryGetValue</c>/<c>Match</c>/<c>Switch</c>/<c>ToString</c>/equality surface every other arity also
/// carries. Mirrors <see cref="OutcomeOfTwoCasesTests"/>'s structure, adapted for a shape with exactly one case
/// and no failure case to branch on. Unlike the generator-emitted arities, <see cref="Outcome{T}"/> is a
/// hand-written type with no implicit conversion from a bare value - construction is always
/// <c>new Outcome&lt;T&gt;(value)</c>.
/// </summary>
public class OutcomeOfOneCaseTests
{
    [Fact]
    public void Construct_FromValue_TryGetValueSucceeds()
    {
        // Arrange & Act
        var result = new Outcome<string>("hello");

        // Assert
        Assert.True(result.TryGetValue(out string _));
    }

    [Fact]
    public void Match_InvokesTheSingleDelegateWithTheValue()
    {
        // Arrange
        var result = new Outcome<string>("hello");

        // Act
        var message = result.Match(value => "value:" + value);

        // Assert
        Assert.Equal("value:hello", message);
    }

    [Fact]
    public void Switch_InvokesTheSingleActionWithTheValue()
    {
        // Arrange
        var result = new Outcome<string>("hello");
        string? observed = null;

        // Act
        result.Switch(value => observed = "value:" + value);

        // Assert
        Assert.Equal("value:hello", observed);
    }

    [Fact]
    public void TryGetValue_AlwaysReturnsTrueAndTheValue()
    {
        // Arrange
        var result = new Outcome<string>("hello");

        // Act
        var found = result.TryGetValue(out string value);

        // Assert
        Assert.True(found);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void ToString_ForwardsToValuesOwnToString()
    {
        // Arrange
        var result = new Outcome<string>("hello");

        // Act & Assert
        Assert.Equal("hello", result.ToString());
    }

    [Fact]
    public void ToString_OverNullValue_ReturnsEmptyString()
    {
        // Arrange
        var result = new Outcome<string?>(null);

        // Act & Assert
        Assert.Equal(string.Empty, result.ToString());
    }

    [Fact]
    public void Value_ReadsTheStronglyTypedPayloadWithoutBoxing()
    {
        // Arrange & Act
        var result = new Outcome<int>(42);

        // Assert - Outcome<T>.Value is T itself, not the boxed object? IUnion.Value.
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ExplicitIUnionValue_ForwardsToTheStronglyTypedValueBoxed()
    {
        // Arrange
        var result = new Outcome<int>(42);

        // Act & Assert
        Assert.Equal(42, ((IUnion)result).Value);
    }

    [Fact]
    public void Equals_TwoInstancesHoldingEqualValues_AreEqualWithEqualHashCodes()
    {
        // Arrange
        var left = new Outcome<string>("hello");
        var right = new Outcome<string>("hello");

        // Act & Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValues_AreUnequal()
    {
        // Arrange
        var left = new Outcome<string>("hello");
        var right = new Outcome<string>("goodbye");

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void Equals_TwoNullPayloadInstances_AreEqual()
    {
        // Arrange
        var left = new Outcome<string?>(null);
        var right = new Outcome<string?>(null);

        // Act & Assert
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_Object_WithUnrelatedType_ReturnsFalse()
    {
        // Arrange
        var outcome = new Outcome<string>("hello");

        // Act & Assert
        object? unrelated = 42;
        object? nothing = null;
        Assert.False(outcome.Equals(unrelated));
        Assert.False(outcome.Equals(nothing));
    }
}
