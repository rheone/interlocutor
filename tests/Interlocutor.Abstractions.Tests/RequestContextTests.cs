namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;

/// <summary>Tests for <see cref="RequestContext"/>.</summary>
public class RequestContextTests
{
    [Fact]
    public void Items_DefaultsToANonNullEmptyInstance()
    {
        // Arrange
        var context = new RequestContext();

        // Act & Assert
        Assert.NotNull(context.Items);
        Assert.Empty(context.Items);
    }

    [Fact]
    public void Items_RepeatedReadsOfTheSameInstance_ReturnTheSameReference()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        var first = context.Items;
        var second = context.Items;

        // Assert - a behavior that writes into Items and a later behavior that reads it must see the same bag.
        Assert.Same(first, second);
    }

    [Fact]
    public void With_WithoutTouchingItems_PreservesTheSameItemsReference()
    {
        // Arrange
        var original = new RequestContext();
        original.Items.Set("key", "value");

        // Act
        var copy = original with
        {
            UserId = "someone",
        };

        // Assert - swapping an unrelated init-only property via 'with' doesn't have to lose what was already
        // written into Items.
        Assert.Same(original.Items, copy.Items);
        Assert.True(copy.Items.TryGet<string>("key", out var value));
        Assert.Equal("value", value);
    }
}
