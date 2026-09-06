namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Covers the generated value equality (<c>Equals</c>, <c>GetHashCode</c>, <c>IEquatable&lt;TSelf&gt;</c>,
/// <c>==</c>/<c>!=</c>) every shipped <see cref="Outcome"/>/<see cref="Outcome{T1, T2}"/>
/// arity carries: delegation to the boxed case value's own equality, union-type identity (no cross-arity or
/// cross-case-type false positives), and composition with the order-independent aggregate equality
/// <see cref="ResultTypes.Failures"/> and its siblings already carry.
/// </summary>
public class OutcomeEqualityTests
{
    [Fact]
    public void Outcome_TwoInstancesHoldingEqualSuccessValues_AreEqualWithEqualHashCodes()
    {
        // Arrange
        Outcome<string, NotFound> left = "hello";
        Outcome<string, NotFound> right = "hello";

        // Act & Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Outcome_TwoInstancesHoldingEqualFailureValues_AreEqualWithEqualHashCodes()
    {
        // Arrange
        Outcome<string, NotFound> left = new NotFound("missing");
        Outcome<string, NotFound> right = new NotFound("missing");

        // Act & Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Outcome_DifferentSuccessValues_AreUnequal()
    {
        // Arrange
        Outcome<string, NotFound> left = "hello";
        Outcome<string, NotFound> right = "goodbye";

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void Outcome_SuccessAndFailure_AreUnequal()
    {
        // Arrange
        Outcome<string, NotFound> success = "hello";
        Outcome<string, NotFound> failure = new NotFound("missing");

        // Act & Assert
        Assert.NotEqual(success, failure);
    }

    [Fact]
    public void Outcome_DefaultInstances_AreEqualToEachOther()
    {
        // Arrange
        var left = default(Outcome<string, NotFound>);
        var right = default(Outcome<string, NotFound>);

        // Act & Assert - a zero-initialized instance's Value is null; both null Values compare equal.
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Outcome_DefaultInstance_IsUnequalToAnyExplicitlyConstructedInstance()
    {
        // Arrange
        var defaultInstance = default(Outcome<string, NotFound>);
        Outcome<string, NotFound> constructed = "hello";

        // Act & Assert
        Assert.NotEqual(defaultInstance, constructed);
    }

    [Fact]
    public void Outcome_EqualsObject_WithUnrelatedType_ReturnsFalse()
    {
        // Arrange
        Outcome<string, NotFound> outcome = "hello";

        // Act & Assert - declared as `object?` so overload resolution can't pick the strongly-typed
        // Equals(TSelf) via the union's own implicit case-value conversion; this exercises the override
        // a caller going through the non-generic object.Equals contract would actually hit.
        object? unrelated = 42;
        object? nothing = null;
        Assert.False(outcome.Equals(unrelated));
        Assert.False(outcome.Equals(nothing));
    }

    [Fact]
    public void Outcome_EqualsObject_BoxedEqualInstance_ReturnsTrue()
    {
        // Arrange
        Outcome<string, NotFound> left = "hello";
        object right = (Outcome<string, NotFound>)"hello";

        // Act & Assert
        Assert.True(left.Equals(right));
    }

    [Fact]
    public void NonGenericOutcome_TwoInstancesHoldingEqualCaseValues_AreEqual()
    {
        // Arrange
        Outcome left = new Failure("boom");
        Outcome right = new Failure("boom");

        // Act & Assert
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void SameCaseValueBoxedIntoDifferentArities_AreNeverEqual()
    {
        // Arrange - "hello" as the success case of a two-case union vs. a three-case union: different
        // runtime types, even though the boxed success value is the same string.
        Outcome<string, NotFound> twoCase = "hello";
        Outcome<string, NotFound, Forbidden> threeCase = "hello";

        // Act & Assert - not the same type, so neither .Equals(object?) can ever return true.
        Assert.False(twoCase.Equals((object)threeCase));
        Assert.False(threeCase.Equals((object)twoCase));
    }

    [Fact]
    public void Outcome_OfThreeCases_GeneratedEquality_DistinguishesEachCaseSlot()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden> success = "hello";
        Outcome<string, NotFound, Forbidden> case2 = new NotFound("missing");
        Outcome<string, NotFound, Forbidden> case3 = new Forbidden("denied");

        // Act & Assert
        Assert.Equal(success, (Outcome<string, NotFound, Forbidden>)"hello");
        Assert.Equal(case2, (Outcome<string, NotFound, Forbidden>)new NotFound("missing"));
        Assert.NotEqual(success, case2);
        Assert.NotEqual(case2, case3);
    }

    [Fact]
    public void Outcome_OfSixCases_GeneratedEquality_HoldsForTheHighestShippedArity()
    {
        // Arrange
        Outcome<string, NotFound, Forbidden, Conflict, Timeout, RateLimited> left = new Conflict("conflict", "CONFLICT_CODE");
        Outcome<string, NotFound, Forbidden, Conflict, Timeout, RateLimited> right = new Conflict("conflict", "CONFLICT_CODE");
        Outcome<string, NotFound, Forbidden, Conflict, Timeout, RateLimited> other = new RateLimited("limit-key");

        // Act & Assert
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, other);
    }

    [Fact]
    public void Outcome_PairedWithFailures_IsEqualRegardlessOfTheOrderItsItemsWereCollectedIn()
    {
        // Arrange - the integration proof: order-independent Failures equality (Items collected in a
        // different order, e.g. concurrent/async work completing out of order) composes correctly through
        // the union's delegation to Value.Equals(other.Value).
        var failureA = new Failure("a");
        var failureB = new Failure("b");
        Outcome<string, Failures> collectedAThenB = new Failures([failureA, failureB]);
        Outcome<string, Failures> collectedBThenA = new Failures([failureB, failureA]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void Outcome_PairedWithFailures_DifferentItems_AreUnequal()
    {
        // Arrange
        Outcome<string, Failures> left = new Failures([new Failure("a"), new Failure("b")]);
        Outcome<string, Failures> right = new Failures([new Failure("a"), new Failure("c")]);

        // Act & Assert
        Assert.NotEqual(left, right);
    }
}
