namespace Interlocutor.CommonKernel.Tests;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Direct <c>Equals</c>/<c>GetHashCode</c>/<c>==</c> coverage for <see cref="Conflicts"/>,
/// <see cref="ValidationErrors"/>, <see cref="ValidationErrors{TErrorCode}"/>, and <see cref="Failures{TErrorCode}"/> -
/// the same order-independent-equality guarantee <see cref="OutcomeEqualityTests"/> already proves for
/// <see cref="Failures"/>, but exercised directly against these types themselves rather than only indirectly
/// through a wrapping <see cref="Outcome{T1, T2}"/>.
/// </summary>
public class AggregateResultTypesEqualityTests
{
    // --- Conflicts ---

    [Fact]
    public void Conflicts_SameItemsDifferentOrder_AreEqualWithMatchingHashCodes()
    {
        // Arrange
        var a = new Conflict("Data changed", "stale.data");
        var b = new Conflict("Version mismatch", "stale.version");
        var collectedAThenB = new Conflicts([a, b]);
        var collectedBThenA = new Conflicts([b, a]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.True(collectedAThenB == collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void Conflicts_DifferentItems_AreUnequal()
    {
        // Arrange
        var left = new Conflicts([new Conflict("Data changed", "stale.data")]);
        var right = new Conflicts([new Conflict("Version mismatch", "stale.version")]);

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void Conflicts_DefaultVsDefault_AreEqual()
    {
        // Arrange & Act & Assert
        Assert.Equal(default(Conflicts), default(Conflicts));
    }

    [Fact]
    public void Conflicts_DefaultVsExplicit_AreUnequal()
    {
        // Arrange
        var explicitValue = new Conflicts([new Conflict("Data changed", "stale.data")]);

        // Act & Assert
        Assert.NotEqual(default, explicitValue);
    }

    // --- ValidationErrors ---

    [Fact]
    public void ValidationErrors_SameItemsDifferentOrder_AreEqualWithMatchingHashCodes()
    {
        // Arrange
        var a = new ValidationError("Email is required");
        var b = new ValidationError("Name is required");
        var collectedAThenB = new ValidationErrors([a, b]);
        var collectedBThenA = new ValidationErrors([b, a]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.True(collectedAThenB == collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void ValidationErrors_DifferentItems_AreUnequal()
    {
        // Arrange
        var left = new ValidationErrors([new ValidationError("Email is required")]);
        var right = new ValidationErrors([new ValidationError("Name is required")]);

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void ValidationErrors_DefaultVsDefault_AreEqual()
    {
        // Arrange & Act & Assert
        Assert.Equal(default(ValidationErrors), default(ValidationErrors));
    }

    [Fact]
    public void ValidationErrors_DefaultVsExplicit_AreUnequal()
    {
        // Arrange
        var explicitValue = new ValidationErrors([new ValidationError("Email is required")]);

        // Act & Assert
        Assert.NotEqual(default, explicitValue);
    }

    // --- ValidationErrors<TErrorCode> ---

    [Fact]
    public void ValidationErrorsGeneric_SameItemsDifferentOrder_AreEqualWithMatchingHashCodes()
    {
        // Arrange
        var a = new ValidationError<int>("Email is required", 1001);
        var b = new ValidationError<int>("Name is required", 1002);
        var collectedAThenB = new ValidationErrors<int>([a, b]);
        var collectedBThenA = new ValidationErrors<int>([b, a]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.True(collectedAThenB == collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void ValidationErrorsGeneric_DifferentItems_AreUnequal()
    {
        // Arrange
        var left = new ValidationErrors<int>([new ValidationError<int>("Email is required", 1001)]);
        var right = new ValidationErrors<int>([new ValidationError<int>("Name is required", 1002)]);

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void ValidationErrorsGeneric_DefaultVsDefault_AreEqual()
    {
        // Arrange & Act & Assert
        Assert.Equal(default(ValidationErrors<int>), default(ValidationErrors<int>));
    }

    [Fact]
    public void ValidationErrorsGeneric_DefaultVsExplicit_AreUnequal()
    {
        // Arrange
        var explicitValue = new ValidationErrors<int>([new ValidationError<int>("Email is required", 1001)]);

        // Act & Assert
        Assert.NotEqual(default, explicitValue);
    }

    // --- Failures<TErrorCode> ---

    [Fact]
    public void FailuresGeneric_SameItemsDifferentOrder_AreEqualWithMatchingHashCodes()
    {
        // Arrange
        var a = new Failure<int>(1001, "Email is required");
        var b = new Failure<int>(1002, "Name is required");
        var collectedAThenB = new Failures<int>([a, b]);
        var collectedBThenA = new Failures<int>([b, a]);

        // Act & Assert
        Assert.Equal(collectedAThenB, collectedBThenA);
        Assert.True(collectedAThenB == collectedBThenA);
        Assert.Equal(collectedAThenB.GetHashCode(), collectedBThenA.GetHashCode());
    }

    [Fact]
    public void FailuresGeneric_DifferentItems_AreUnequal()
    {
        // Arrange
        var left = new Failures<int>([new Failure<int>(1001, "Email is required")]);
        var right = new Failures<int>([new Failure<int>(1002, "Name is required")]);

        // Act & Assert
        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void FailuresGeneric_DefaultVsDefault_AreEqual()
    {
        // Arrange & Act & Assert
        Assert.Equal(default(Failures<int>), default(Failures<int>));
    }

    [Fact]
    public void FailuresGeneric_DefaultVsExplicit_AreUnequal()
    {
        // Arrange
        var explicitValue = new Failures<int>([new Failure<int>(1001, "Email is required")]);

        // Act & Assert
        Assert.NotEqual(default, explicitValue);
    }
}
