namespace Interlocutor.Persistence.Abstractions.Tests;

using System.Diagnostics;
using System.Reflection;
using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>Count</c>/<c>RootHidden</c> debugger pairing added to <see cref="Page{T}"/> (ticket 06).
/// </summary>
public class PageDebuggerDisplayTests
{
    [Fact]
    public void Page_DebuggerDisplay_ReferencesRealMembers() => DebuggerDisplayVerifier.AssertValid(typeof(Page<string>));

    [Fact]
    public void Page_Items_IsRootHidden()
    {
        // Arrange
        var itemsProperty = typeof(Page<string>).GetProperty(nameof(Page<string>.Items));

        // Act
        var attribute = itemsProperty?.GetCustomAttribute<DebuggerBrowsableAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(DebuggerBrowsableState.RootHidden, attribute.State);
    }
}
