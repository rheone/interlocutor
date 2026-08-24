namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;

/// <summary>Tests for <see cref="HandlerOverrideAttribute"/>.</summary>
public class HandlerOverrideAttributeTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_SetsHandlerType()
    {
        // Arrange & Act
        var attribute = new HandlerOverrideAttribute(typeof(string));

        // Assert
        Assert.Equal(typeof(string), attribute.HandlerType);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AttributeUsage_AllowsMultipleAndIsNotInherited()
    {
        // Arrange
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(typeof(HandlerOverrideAttribute), typeof(AttributeUsageAttribute))!;

        // Act & Assert - the attribute must be applicable more than once per class (multiple overridden
        // handlers) and must not be inherited, matching the documented "escape hatch" design.
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }
}
