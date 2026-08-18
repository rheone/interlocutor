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
        var registry = DefaultOutcomeCaseRegistry.Instance;

        var response = registry.Construct<OperationOutcome<string, Failure>, Failure>(new Failure("boom"));

        var failure = Assert.IsType<Failure>(response.Value);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void Construct_RealUnion_MissingCase_ThrowsOutcomeCaseNotSupportedException()
    {
        var registry = DefaultOutcomeCaseRegistry.Instance;

        var ex = Assert.Throws<OutcomeCaseNotSupportedException>(() =>
            registry.Construct<OperationOutcome<string, Failure>, NotFound>(default)
        );

        Assert.Equal(typeof(OperationOutcome<string, Failure>), ex.ResponseType);
        Assert.Equal(typeof(NotFound), ex.CaseType);
        Assert.Contains(nameof(NotFound), ex.Message);
    }

    [Fact]
    public void TryGetActiveCase_RealUnion_ExtractsActiveCaseTypeAndValue()
    {
        var registry = DefaultOutcomeCaseRegistry.Instance;
        OperationOutcome<string, Failure> response = new Failure("boom");

        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        Assert.True(found);
        Assert.Equal(typeof(Failure), caseType);
        var failure = Assert.IsType<Failure>(caseValue);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void TryGetActiveCase_RealUnion_SuccessCase_ExtractsSuccessValue()
    {
        var registry = DefaultOutcomeCaseRegistry.Instance;
        OperationOutcome<string, Failure> response = "ok";

        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Equal("ok", caseValue);
    }

    [Fact]
    public void TryGetActiveCase_NullResponse_ReturnsFalseWithoutThrowing()
    {
        var registry = DefaultOutcomeCaseRegistry.Instance;
        HandRolledOutcome nullResponse = null!;

        var found = registry.TryGetActiveCase(nullResponse, out var caseType, out var caseValue);

        Assert.False(found);
        Assert.Null(caseValue);
        Assert.Equal(typeof(HandRolledOutcome), caseType);
    }

    [Fact]
    public void TryGetActiveCase_NonUnionResponse_ThrowsOutcomeCaseNotSupportedException()
    {
        var registry = DefaultOutcomeCaseRegistry.Instance;
        var response = HandRolledOutcome.FromSuccess("ok");

        var ex = Assert.Throws<OutcomeCaseNotSupportedException>(() => registry.TryGetActiveCase(response, out _, out _));

        Assert.Equal(typeof(HandRolledOutcome), ex.ResponseType);
        Assert.Null(ex.CaseType);
    }

    // --- OutcomeCaseRegistryBuilder against a hand-rolled, non-union IOperationOutcome ---

    [Fact]
    public void Construct_RegisteredCustomFactory_UsedInsteadOfDefault()
    {
        var registry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<HandRolledOutcome, string>(HandRolledOutcome.FromSuccess)
            .RegisterFactory<HandRolledOutcome, Failure>(HandRolledOutcome.FromFailure)
            .Build();

        var response = registry.Construct<HandRolledOutcome, Failure>(new Failure("boom"));

        Assert.Equal(typeof(Failure), response.CaseType);
        var failure = Assert.IsType<Failure>(response.CaseValue);
        Assert.Equal("boom", failure.Reason);
    }

    [Fact]
    public void TryGetActiveCase_RegisteredCustomExtractor_UsedInsteadOfDefault()
    {
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
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        Assert.True(found);
        Assert.Equal(typeof(string), caseType);
        Assert.Equal("ok", caseValue);
    }

    [Fact]
    public void Construct_UnregisteredResponseType_FallsBackToDefaultAndThrowsForNonUnionType()
    {
        // HandRolledOutcome has no factory registered on this builder, so Construct falls back to
        // DefaultOutcomeCaseRegistry, which cannot handle a non-union type reflectively.
        var registry = new OutcomeCaseRegistryBuilder().Build();

        Assert.Throws<OutcomeCaseNotSupportedException>(() =>
            registry.Construct<HandRolledOutcome, Failure>(new Failure("boom"))
        );
    }

    [Fact]
    public void Construct_UnregisteredResponseType_FallsBackToDefaultForRealUnion()
    {
        // A real union works through the builder-produced registry even with nothing registered for it,
        // because the fallback is DefaultOutcomeCaseRegistry's reflection-based construction.
        var registry = new OutcomeCaseRegistryBuilder().Build();

        var response = registry.Construct<OperationOutcome<string, Failure>, Failure>(new Failure("boom"));

        var failure = Assert.IsType<Failure>(response.Value);
        Assert.Equal("boom", failure.Reason);
    }

    /// <summary>A hand-rolled, non-union <see cref="IOperationOutcome"/> implementation - the shape
    /// <see cref="DefaultOutcomeCaseRegistry"/> cannot handle reflectively (no compiler-generated case
    /// constructor, no <c>IUnion</c> marker), and so must be covered by a custom factory/extractor
    /// registered via <see cref="OutcomeCaseRegistryBuilder"/>.</summary>
    public sealed class HandRolledOutcome : IOperationOutcome
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
