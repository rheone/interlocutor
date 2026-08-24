namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;

/// <summary>Tests for <see cref="AmbientRequestContextAccessor"/>.</summary>
public class AmbientRequestContextAccessorTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Context_WhenNeverSet_IsNull()
    {
        // Arrange
        var accessor = new AmbientRequestContextAccessor();

        // Act & Assert
        Assert.Null(accessor.Context);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void Context_WhenSet_ReturnsTheAssignedValue()
    {
        // Arrange
        var accessor = new AmbientRequestContextAccessor();
        var context = new RequestContext { CorrelationId = "corr-1" };

        // Act
        accessor.Context = context;

        // Assert
        Assert.Same(context, accessor.Context);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void Context_SetToNullAfterBeingPopulated_ReturnsNullAgain()
    {
        // Arrange - the accessor is null-safe: clearing a previously populated context must be observable,
        // not just leaving the old value behind.
        var accessor = new AmbientRequestContextAccessor { Context = new RequestContext { CorrelationId = "corr-1" } };

        // Act
        accessor.Context = null;

        // Assert
        Assert.Null(accessor.Context);
    }
}
