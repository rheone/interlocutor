namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Notifications;

/// <summary>Tests for <see cref="NotificationHandlerRunBeforeAttribute"/>.</summary>
public class NotificationHandlerRunBeforeAttributeTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_SetsHandlerType()
    {
        // Arrange & Act
        var attribute = new NotificationHandlerRunBeforeAttribute(typeof(string));

        // Assert
        Assert.Equal(typeof(string), attribute.HandlerType);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AttributeUsage_AllowsMultipleAndIsNotInherited()
    {
        // Arrange
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(typeof(NotificationHandlerRunBeforeAttribute), typeof(AttributeUsageAttribute))!;

        // Act & Assert - must be applicable more than once per handler (multiple successors) and must not be
        // inherited, matching NotificationHandlerRunAfterAttribute's mirrored design.
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }
}
