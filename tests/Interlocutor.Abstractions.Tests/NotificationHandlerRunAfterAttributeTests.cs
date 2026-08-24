namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Notifications;

/// <summary>Tests for <see cref="NotificationHandlerRunAfterAttribute"/>.</summary>
public class NotificationHandlerRunAfterAttributeTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_SetsHandlerType()
    {
        // Arrange & Act
        var attribute = new NotificationHandlerRunAfterAttribute(typeof(string));

        // Assert
        Assert.Equal(typeof(string), attribute.HandlerType);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AttributeUsage_AllowsMultipleAndIsNotInherited()
    {
        // Arrange
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(typeof(NotificationHandlerRunAfterAttribute), typeof(AttributeUsageAttribute))!;

        // Act & Assert - must be applicable more than once per handler (multiple predecessors) and must not
        // be inherited, matching NotificationHandlerRunBeforeAttribute's mirrored design.
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }
}
