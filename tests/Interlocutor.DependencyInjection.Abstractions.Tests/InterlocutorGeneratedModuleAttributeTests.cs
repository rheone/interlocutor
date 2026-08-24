namespace Interlocutor.DependencyInjection.Abstractions.Tests;

// Auto Generated, verify expected behavior:
// InterlocutorGeneratedModuleAttribute had no direct test coverage - only exercised by the generator itself
// (not part of this project) and referenced in doc comments elsewhere in this project's tests. Covers the
// constructor's ModuleType assignment and its ArgumentNullException.ThrowIfNull(moduleType) guard.
public class InterlocutorGeneratedModuleAttributeTests
{
    [Fact]
    public void Constructor_AssignsModuleType()
    {
        // Arrange & Act
        var attribute = new InterlocutorGeneratedModuleAttribute(typeof(SampleModule));

        // Assert
        Assert.Equal(typeof(SampleModule), attribute.ModuleType);
    }

    [Fact]
    public void Constructor_WithNullModuleType_ThrowsArgumentNullException()
    {
        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => new InterlocutorGeneratedModuleAttribute(null!));

        // Assert
        Assert.Equal("moduleType", ex.ParamName);
    }

    private static class SampleModule;
}
