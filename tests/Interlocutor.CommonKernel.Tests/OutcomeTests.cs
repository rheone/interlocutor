namespace Interlocutor.CommonKernel.Tests;

using System.Runtime.CompilerServices;

/// <summary>
/// Covers the case-less, non-generic <see cref="Outcome"/>: it satisfies the <see cref="IUnion"/> response
/// constraint, its <see cref="IUnion.Value"/> is always <see langword="null"/>, and its <c>ToString()</c> is a
/// fixed literal. Its <c>IOutcomeCaseRegistry</c> behavior - <c>Construct</c> failing clearly for any case,
/// <c>TryGetActiveCase</c> reporting no active case - is covered in <c>Interlocutor.Abstractions.Tests</c>,
/// the package that owns that registry.
/// </summary>
public class OutcomeTests
{
    [Fact]
    public void Outcome_SatisfiesIUnionConstraint()
    {
        // Arrange & Act
        var result = default(Outcome);

        // Assert - the compile-time proof is the assignment itself: a type that didn't implement IUnion
        // would fail to compile here.
        Assert.IsAssignableFrom<IUnion>(result);
    }

    [Fact]
    public void Outcome_Value_IsAlwaysNull()
    {
        // Arrange - default-initialized is no different from any other instance, since there is no state to
        // initialize either way (see Outcome's own remarks).
        IUnion result = default(Outcome);

        // Act & Assert
        Assert.Null(result.Value);
    }

    [Fact]
    public void ToString_ReturnsFixedLiteral()
    {
        // Arrange
        var result = default(Outcome);

        // Act & Assert
        Assert.Equal("Outcome", result.ToString());
    }
}
