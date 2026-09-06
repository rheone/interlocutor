namespace Interlocutor.Abstractions.Tests;

using System.Runtime.CompilerServices;
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
    public void TryGetActiveCase_HandRolledIUnionImplementation_ExtractsActiveCaseWithoutRegistration()
    {
        // DefaultOutcomeCaseRegistry recognizes any IUnion implementer generically, even a hand-rolled one
        // with no compiler-generated case constructor - extraction reads HandRolledOutcome's own
        // IUnion.Value directly, needing no custom extractor registered via OutcomeCaseRegistryBuilder.
        // Arrange
        var registry = DefaultOutcomeCaseRegistry.Instance;
        var response = HandRolledOutcome.FromSuccess("ok");

        // Act
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Equal("ok", caseValue);
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

    /// <summary>A hand-rolled <see cref="IOutcome"/>/<see cref="IUnion"/> implementation that is not a
    /// compiler-generated <c>union</c> - <see cref="DefaultOutcomeCaseRegistry"/> can extract its active case
    /// via <see cref="IUnion.Value"/> alone, but construction still needs a case constructor
    /// <see cref="Interlocutor.Abstractions.Pipeline.UnionCaseFactory"/> can find reflectively, which this type has none of, so a custom
    /// factory registered via <see cref="OutcomeCaseRegistryBuilder"/> is required to construct one.</summary>
    public sealed class HandRolledOutcome : IOutcome, IUnion
    {
        private HandRolledOutcome(Type caseType, object? caseValue)
        {
            this.CaseType = caseType;
            this.CaseValue = caseValue;
        }

        public Type CaseType { get; }

        public object? CaseValue { get; }

        object? IUnion.Value => this.CaseValue;

        public static HandRolledOutcome FromSuccess(string value) => new(typeof(string), value);

        public static HandRolledOutcome FromFailure(Failure failure) => new(typeof(Failure), failure);
    }
}
