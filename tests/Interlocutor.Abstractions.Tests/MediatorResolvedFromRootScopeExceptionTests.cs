namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;

/// <summary>Tests for <see cref="MediatorResolvedFromRootScopeException"/>.</summary>
public class MediatorResolvedFromRootScopeExceptionTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_SetsRequestType()
    {
        // Arrange & Act
        var ex = new MediatorResolvedFromRootScopeException(typeof(TestQuery));

        // Assert
        Assert.Equal(typeof(TestQuery), ex.RequestType);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void Constructor_BuildsMessageNamingTheRequestTypeAndTheFix()
    {
        // Arrange & Act
        var ex = new MediatorResolvedFromRootScopeException(typeof(TestQuery));

        // Assert
        Assert.Contains(typeof(TestQuery).FullName!, ex.Message);
        Assert.Contains("IServiceScopeFactory", ex.Message);
    }
}
