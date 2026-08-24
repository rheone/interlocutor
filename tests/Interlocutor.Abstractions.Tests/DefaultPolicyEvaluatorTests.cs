namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;

/// <summary>Tests for <see cref="DefaultPolicyEvaluator"/>.</summary>
public class DefaultPolicyEvaluatorTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task EvaluateAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var evaluator = new DefaultPolicyEvaluator();

        // Act
        var result = await evaluator.EvaluateAsync("any-policy", new object(), CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void Instance_IsASingletonDefaultPolicyEvaluator()
    {
        // Arrange & Act & Assert - Instance exists as a static field to avoid allocating a new grant-all
        // evaluator per resolution; every access must observe the same object.
        Assert.Same(DefaultPolicyEvaluator.Instance, DefaultPolicyEvaluator.Instance);
    }
}
