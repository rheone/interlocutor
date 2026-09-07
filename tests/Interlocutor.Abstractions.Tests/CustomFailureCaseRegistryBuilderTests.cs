namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Mapping;

/// <summary>Tests for <see cref="CustomFailureCaseRegistryBuilder{TResult}"/> and the
/// <see cref="ICustomFailureCaseRegistry{TResult}"/> it builds.</summary>
public class CustomFailureCaseRegistryBuilderTests
{
    [Fact]
    public void TryMap_RegisteredType_ReturnsTrueAndTheMappedResult()
    {
        // Arrange
        var registry = new CustomFailureCaseRegistryBuilder<string>()
            .RegisterType<PaymentDeclined>(declined => $"declined: {declined.Reason}")
            .Build();

        // Act
        var found = registry.TryMap(new PaymentDeclined("insufficient funds"), out var result);

        // Assert
        Assert.True(found);
        Assert.Equal("declined: insufficient funds", result);
    }

    [Fact]
    public void TryMap_ByTypeRegisteredMapping_ReturnsTrueAndTheMappedResult()
    {
        // Arrange
        var registry = new CustomFailureCaseRegistryBuilder<string>()
            .RegisterType(
                typeof(PaymentDeclined),
                new Func<PaymentDeclined, string>(declined => $"declined: {declined.Reason}")
            )
            .Build();

        // Act
        var found = registry.TryMap(new PaymentDeclined("expired card"), out var result);

        // Assert
        Assert.True(found);
        Assert.Equal("declined: expired card", result);
    }

    [Fact]
    public void RegisterType_ByType_MismatchedDelegateShape_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CustomFailureCaseRegistryBuilder<string>();

        // Act
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.RegisterType(typeof(PaymentDeclined), new Func<InventoryShortage, string>(shortage => shortage.Sku))
        );

        // Assert
        Assert.Equal("mapping", ex.ParamName);
    }

    [Fact]
    public void RegisterType_SecondRegistrationForSameType_OverridesTheFirst()
    {
        // Arrange - last-write-wins, matching OutcomeClassifierBuilder's documented override behavior.
        var registry = new CustomFailureCaseRegistryBuilder<string>()
            .RegisterType<PaymentDeclined>(declined => $"first: {declined.Reason}")
            .RegisterType<PaymentDeclined>(declined => $"second: {declined.Reason}")
            .Build();

        // Act
        registry.TryMap(new PaymentDeclined("insufficient funds"), out var result);

        // Assert
        Assert.Equal("second: insufficient funds", result);
    }

    [Fact]
    public void RegisterType_GenericOverrideByTypeOverload_OverridesTheFirst()
    {
        // Arrange - the two RegisterType overloads share one underlying table, so a by-Type registration
        // overrides an earlier generic registration for the same runtime type, and vice versa.
        var registry = new CustomFailureCaseRegistryBuilder<string>()
            .RegisterType<PaymentDeclined>(declined => $"generic: {declined.Reason}")
            .RegisterType(typeof(PaymentDeclined), new Func<PaymentDeclined, string>(declined => $"byType: {declined.Reason}"))
            .Build();

        // Act
        registry.TryMap(new PaymentDeclined("insufficient funds"), out var result);

        // Assert
        Assert.Equal("byType: insufficient funds", result);
    }

    [Fact]
    public void TryMap_UnregisteredType_ReturnsFalseRatherThanThrowing()
    {
        // Arrange
        var registry = new CustomFailureCaseRegistryBuilder<string>()
            .RegisterType<PaymentDeclined>(declined => declined.Reason)
            .Build();

        // Act
        var found = registry.TryMap(new InventoryShortage("SKU-1", 5), out var result);

        // Assert
        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryMap_EmptyRegistry_ReturnsFalse()
    {
        // Arrange
        var registry = new CustomFailureCaseRegistryBuilder<string>().Build();

        // Act
        var found = registry.TryMap(new PaymentDeclined("insufficient funds"), out var result);

        // Assert
        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void RegisterType_ReturnsSameBuilderInstance_ForChaining()
    {
        // Arrange
        var builder = new CustomFailureCaseRegistryBuilder<string>();

        // Act
        var returned = builder.RegisterType<PaymentDeclined>(declined => declined.Reason);

        // Assert
        Assert.Same(builder, returned);
    }

    private sealed record PaymentDeclined(string Reason);

    private sealed record InventoryShortage(string Sku, int Requested);
}
