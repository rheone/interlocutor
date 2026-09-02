namespace Interlocutor.CommonKernel.Tests;

using System.Diagnostics;
using System.Reflection;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.TestSupport;

/// <summary>
/// Verifies the <c>Count</c>/<c>RootHidden</c> debugger pairing added to the internal <c>DistinctItemSet&lt;TItem&gt;</c>
/// storage type, and the <c>[DebuggerBrowsable(Never)]</c> hiding its composing field(s) on every plural
/// aggregate result type <c>Interlocutor.CommonKernel</c> ships (ticket 06).
/// </summary>
public class CollectionMetadataDebuggerDisplayTests
{
    private const BindingFlags PrivateInstanceFields = BindingFlags.NonPublic | BindingFlags.Instance;

    // DistinctItemSet<TItem> is internal, so it's looked up by name via reflection rather than referenced
    // directly.
    private static readonly Type DistinctItemSetType = typeof(Failures).Assembly.GetType(
        "Interlocutor.CommonKernel.ResultTypes.Internal.DistinctItemSet`1",
        throwOnError: true
    )!;

    [Fact]
    public void DistinctItemSet_DebuggerDisplay_ReferencesRealMembers() =>
        DebuggerDisplayVerifier.AssertValid(DistinctItemSetType);

    [Fact]
    public void DistinctItemSet_Items_IsRootHidden() =>
        AssertBrowsable(DistinctItemSetType.GetProperty("Items")!, DebuggerBrowsableState.RootHidden);

    [Theory]
    [InlineData(typeof(Failures), "_failures")]
    [InlineData(typeof(Failures<int>), "_failures")]
    [InlineData(typeof(Failures<int>), "_uniqueErrorCodes")]
    [InlineData(typeof(Conflicts), "_conflicts")]
    [InlineData(typeof(ValidationErrors), "_errors")]
    [InlineData(typeof(ValidationErrors<int>), "_errors")]
    [InlineData(typeof(ValidationErrors<int>), "_uniqueMessages")]
    [InlineData(typeof(ValidationErrors<int>), "_uniqueErrorCodes")]
    public void AggregateType_ComposedDistinctItemSetField_IsNeverBrowsable(Type aggregateType, string fieldName)
    {
        // Arrange
        var field = aggregateType.GetField(fieldName, PrivateInstanceFields);

        // Assert
        Assert.NotNull(field);
        AssertBrowsable(field, DebuggerBrowsableState.Never);
    }

    private static void AssertBrowsable(MemberInfo? member, DebuggerBrowsableState expected)
    {
        var attribute = member?.GetCustomAttribute<DebuggerBrowsableAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expected, attribute.State);
    }
}
