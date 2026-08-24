namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="OutcomeCaseRegistryExtensions"/>.</summary>
public class OutcomeCaseRegistryExtensionsTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void ConstructOrThrow_ResponseTypeHasTheCase_ReturnsConstructedResponse()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;

        // Act
        var response = registry.ConstructOrThrow<OperationOutcome<string, Failure>, Failure>(
            new Failure("boom"),
            "SomeBehavior",
            "denied a request",
            "A response type with a Failure case is required"
        );

        // Assert
        var failure = Assert.IsType<Failure>(response.Value);
        Assert.Equal("boom", failure.Reason);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void ConstructOrThrow_ResponseTypeHasNoSuchCase_ThrowsInvalidOperationExceptionNamingBehaviorAndRequirement()
    {
        // Arrange - OperationOutcome<string, Failure> has no NotFound case to construct.
        var registry = DefaultOutcomeCaseRegistry.Instance;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.ConstructOrThrow<OperationOutcome<string, Failure>, NotFound>(
                default,
                "SomeBehavior<Query, OperationOutcome<string, Failure>>",
                "denied a request",
                "A response type with a NotFound case is required"
            )
        );

        // Assert
        Assert.Contains("SomeBehavior<Query, OperationOutcome<string, Failure>>", ex.Message);
        Assert.Contains("denied a request", ex.Message);
        Assert.Contains("A response type with a NotFound case is required", ex.Message);
        Assert.IsType<OutcomeCaseNotSupportedException>(ex.InnerException);
    }
}
