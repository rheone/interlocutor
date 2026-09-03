namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="IOutcomeCaseRegistry"/>, <see cref="DefaultOutcomeCaseRegistry"/>, and
/// <see cref="OutcomeCaseRegistryBuilder"/>.</summary>
public class OutcomeCaseRegistryTests
{
    // --- DefaultOutcomeCaseRegistry against a real compiler-generated union ---

    [Fact]
    public void Construct_RealUnion_ExistingCase_BuildsResponseWithThatCase()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;

        // Act
        var response = registry.Construct<Outcome<string, Failure>, Failure>(new Failure("boom"));

        // Assert
        var failure = Assert.IsType<Failure>(response.Value);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void Construct_RealUnion_MissingCase_ThrowsOutcomeCaseNotSupportedException()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;

        // Act
        var ex = Assert.Throws<OutcomeCaseNotSupportedException>(() =>
            registry.Construct<Outcome<string, Failure>, NotFound>(default)
        );

        // Assert
        Assert.Equal(typeof(Outcome<string, Failure>), ex.ResponseType);
        Assert.Equal(typeof(NotFound), ex.CaseType);
        Assert.Contains(nameof(NotFound), ex.Message);
    }

    [Fact]
    public void TryGetActiveCase_RealUnion_ExtractsActiveCaseTypeAndValue()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;
        Outcome<string, Failure> response = new Failure("boom");

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(Failure), caseType);
        var failure = Assert.IsType<Failure>(caseValue);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void TryGetActiveCase_RealUnion_NullPayloadSuccess_ExtractsSuccessCaseTypeNotContainerType()
    {
        // Arrange - a legitimately null success payload (see finding 07): the reported case type must be the
        // success case's own declared type, not Outcome<string?, NotFound> itself.
        var registry = DefaultOutcomeCaseRegistry.Instance;
        Outcome<string?, NotFound> response = (string?)null;

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Null(caseValue);
    }

    [Fact]
    public void TryGetActiveCase_GenuinelyUnsetUnion_ReturnsFalse()
    {
        // Arrange - neither case type of Outcome<Success, Failure> can ever be null, so a default (never
        // constructed) union is still distinguishable from a null-payload success.
        var registry = DefaultOutcomeCaseRegistry.Instance;
        var response = default(Outcome<Success, Failure>);

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.False(found);
        Assert.Equal(typeof(Outcome<Success, Failure>), caseType);
        Assert.Null(caseValue);
    }

    [Fact]
    public void TryGetActiveCase_RealUnion_SuccessCase_ExtractsSuccessValue()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;
        Outcome<string, Failure> response = "ok";

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Equal("ok", caseValue);
    }

    [Fact]
    public void TryGetActiveCase_NullResponse_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;
        HandRolledOutcome nullResponse = null!;

        // Act
        var found = registry.TryGetActiveCase(nullResponse, out var caseType, out var caseValue);

        // Assert
        Assert.False(found);
        Assert.Null(caseValue);
        Assert.Equal(typeof(HandRolledOutcome), caseType);
    }

    [Fact]
    public void TryGetActiveCase_NonUnionResponse_ThrowsOutcomeCaseNotSupportedException()
    {
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;
        var response = HandRolledOutcome.FromSuccess("ok");

        // Act
        var ex = Assert.Throws<OutcomeCaseNotSupportedException>(() => registry.TryGetActiveCase(response, out _, out _));

        // Assert
        Assert.Equal(typeof(HandRolledOutcome), ex.ResponseType);
        Assert.Null(ex.CaseType);
    }

    // --- OutcomeCaseRegistryBuilder against a hand-rolled, non-union IOutcome ---

    [Fact]
    public void Construct_RegisteredCustomFactory_UsedInsteadOfDefault()
    {
        // Arrange
        var registry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<HandRolledOutcome, string>(HandRolledOutcome.FromSuccess)
            .RegisterFactory<HandRolledOutcome, Failure>(HandRolledOutcome.FromFailure)
            .Build();

        // Act
        var response = registry.Construct<HandRolledOutcome, Failure>(new Failure("boom"));

        // Assert
        Assert.Equal(typeof(Failure), response.CaseType);
        var failure = Assert.IsType<Failure>(response.CaseValue);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void TryGetActiveCase_RegisteredCustomExtractor_UsedInsteadOfDefault()
    {
        // Arrange
        var registry = new OutcomeCaseRegistryBuilder()
            .RegisterExtractor<HandRolledOutcome>(
                (HandRolledOutcome response, out Type caseType, out object? caseValue) =>
                {
                    caseType = response.CaseType;
                    caseValue = response.CaseValue;
                    return true;
                }
            )
            .Build();
        var response = HandRolledOutcome.FromSuccess("ok");

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Equal("ok", caseValue);
    }

    [Fact]
    public void Construct_UnregisteredResponseType_FallsBackToDefaultAndThrowsForNonUnionType()
    {
        // Arrange - HandRolledOutcome has no factory registered on this builder, so Construct falls back to
        // DefaultOutcomeCaseRegistry, which cannot handle a non-union type reflectively.
        var registry = new OutcomeCaseRegistryBuilder().Build();

        // Act & Assert
        Assert.Throws<OutcomeCaseNotSupportedException>(() =>
            registry.Construct<HandRolledOutcome, Failure>(new Failure("boom"))
        );
    }

    [Fact]
    public void Construct_UnregisteredResponseType_FallsBackToDefaultForRealUnion()
    {
        // Arrange - A real union works through the builder-produced registry even with nothing registered
        // for it, because the fallback is DefaultOutcomeCaseRegistry's reflection-based construction.
        var registry = new OutcomeCaseRegistryBuilder().Build();

        // Act
        var response = registry.Construct<Outcome<string, Failure>, Failure>(new Failure("boom"));

        // Assert
        var failure = Assert.IsType<Failure>(response.Value);
        Assert.Equal("boom", failure.Reason);
    }

    /// <summary>A hand-rolled, non-union <see cref="IOutcome"/> implementation - the shape
    /// <see cref="DefaultOutcomeCaseRegistry"/> cannot handle reflectively (no compiler-generated case
    /// constructor, no <c>IUnion</c> marker), and so must be covered by a custom factory/extractor
    /// registered via <see cref="OutcomeCaseRegistryBuilder"/>.</summary>
    public sealed class HandRolledOutcome : IOutcome
    {
        private HandRolledOutcome(Type caseType, object? caseValue)
        {
            this.CaseType = caseType;
            this.CaseValue = caseValue;
        }

        public Type CaseType { get; }

        public object? CaseValue { get; }

        public static HandRolledOutcome FromSuccess(string value) => new(typeof(string), value);

        public static HandRolledOutcome FromFailure(Failure failure) => new(typeof(Failure), failure);
    }
}
