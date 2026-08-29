namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;

/// <summary>Tests for <see cref="RequestContextItems"/>.</summary>
public class RequestContextItemsTests
{
    [Fact]
    public void SetThenTryGet_RoundTripsTheValue()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act
        items.Set("key", 42);
        var found = items.TryGet<int>("key", out var value);

        // Assert
        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGet_KeyNotPresent_ReturnsFalse()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act
        var found = items.TryGet<int>("missing", out var value);

        // Assert
        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGet_StoredValueIsADifferentType_ReturnsFalse()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("key", "a string");

        // Act
        var found = items.TryGet<int>("key", out var value);

        // Assert
        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Indexer_SetThenGet_RoundTripsTheValue()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act
        items["key"] = "value";

        // Assert
        Assert.Equal("value", items["key"]);
    }

    [Fact]
    public void Indexer_KeyNotPresent_ReturnsNull()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act & Assert
        Assert.Null(items["missing"]);
    }

    [Fact]
    public void Remove_RemovesAPreviouslySetKey()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("key", "value");

        // Act
        var removed = items.Remove("key");

        // Assert
        Assert.True(removed);
        Assert.False(items.ContainsKey("key"));
    }

    [Fact]
    public void Remove_KeyNotPresent_ReturnsFalse()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act & Assert
        Assert.False(items.Remove("missing"));
    }

    [Fact]
    public void ContainsKey_ReflectsWhetherAKeyIsSet()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("key", "value");

        // Act & Assert
        Assert.True(items.ContainsKey("key"));
        Assert.False(items.ContainsKey("other"));
    }

    [Fact]
    public void Clear_RemovesEveryEntry()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("a", 1);
        items.Set("b", 2);

        // Act
        items.Clear();

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public void GetOrAdd_KeyNotPresent_StoresAndReturnsTheFactoryValue()
    {
        // Arrange
        var items = new RequestContextItems();

        // Act
        var value = items.GetOrAdd("key", _ => "created");

        // Assert
        Assert.Equal("created", value);
        Assert.True(items.TryGet<string>("key", out var stored));
        Assert.Equal("created", stored);
    }

    [Fact]
    public void GetOrAdd_KeyAlreadyPresent_ReturnsTheExistingValueWithoutCallingTheFactory()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("key", "existing");
        var factoryCalled = false;

        // Act
        var value = items.GetOrAdd(
            "key",
            _ =>
            {
                factoryCalled = true;
                return "created";
            }
        );

        // Assert
        Assert.Equal("existing", value);
        Assert.False(factoryCalled);
    }

    [Fact]
    public void Constructor_WithSeed_PopulatesFromTheSeed()
    {
        // Arrange
        var seed = new[] { new KeyValuePair<string, object?>("a", 1), new KeyValuePair<string, object?>("b", 2) };

        // Act
        var items = new RequestContextItems(seed);

        // Assert
        Assert.True(items.TryGet<int>("a", out var a));
        Assert.Equal(1, a);
        Assert.True(items.TryGet<int>("b", out var b));
        Assert.Equal(2, b);
    }

    [Fact]
    public void GetEnumerator_EnumeratesEveryStoredEntry()
    {
        // Arrange
        var items = new RequestContextItems();
        items.Set("a", 1);
        items.Set("b", 2);

        // Act
        var keys = items.Select(kvp => kvp.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();

        // Assert
        Assert.Equal(["a", "b"], keys);
    }

    [Fact]
    public async Task Set_CalledConcurrentlyWithDistinctKeys_LosesNoWrites()
    {
        // Arrange - proves the thread-safety this type's remarks promise: nothing prevents a pipeline
        // behavior from forking work internally within an ordinary dispatch.
        var items = new RequestContextItems();
        const int concurrentWrites = 200;

        // Act
        await Task.WhenAll(Enumerable.Range(0, concurrentWrites).Select(i => Task.Run(() => items.Set($"key-{i}", i))));

        // Assert
        for (var i = 0; i < concurrentWrites; i++)
        {
            Assert.True(items.TryGet<int>($"key-{i}", out var value));
            Assert.Equal(i, value);
        }
    }
}
