namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Exercises <see cref="PipelineOverrideRegistry"/> - the container-neutral <see cref="IPipelineOverrideRegistry"/>
/// implementation built from a builder's declared <see cref="PipelineOverrideRule"/> list - standalone.
/// <see cref="PipelineChainResolver"/>'s own resolution behavior is covered against a fake registry in
/// <c>Interlocutor.Abstractions.Tests.PipelineChainResolverTests</c>; this file covers only what
/// <see cref="PipelineOverrideRegistry"/> itself returns.
/// </summary>
public class PipelineOverrideRegistryTests
{
    [Fact]
    public void TryGetOverride_NoRuleMatches_ReturnsNull()
    {
        // Arrange
        var registry = new PipelineOverrideRegistry([
            new PipelineOverrideRule(t => t == typeof(OtherRequest), "other-rule", [typeof(SampleBehavior<,>)]),
        ]);

        // Act
        var match = registry.TryGetOverride(typeof(SampleRequest));

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void TryGetOverride_OneRuleMatches_ReturnsItsBehaviorTypesAndName()
    {
        // Arrange
        var registry = new PipelineOverrideRegistry([
            new PipelineOverrideRule(t => t == typeof(SampleRequest), "sample-rule", [typeof(SampleBehavior<,>)]),
        ]);

        // Act
        var match = registry.TryGetOverride(typeof(SampleRequest));

        // Assert
        Assert.NotNull(match);
        Assert.Equal([typeof(SampleBehavior<,>)], match.BehaviorTypes);
        Assert.Equal("sample-rule", match.RuleName);
    }

    [Fact]
    public void TryGetOverride_MultipleRulesMatch_ReturnsFirstDeclaredRule()
    {
        // Arrange - a real conflict is a build-validation error (see ConflictingPipelineOverride); absent
        // that check, this registry still needs a deterministic answer: first declared wins.
        var registry = new PipelineOverrideRegistry([
            new PipelineOverrideRule(t => t == typeof(SampleRequest), "first-rule", [typeof(SampleBehavior<,>)]),
            new PipelineOverrideRule(t => t == typeof(SampleRequest), "second-rule", [typeof(OtherBehavior<,>)]),
        ]);

        // Act
        var match = registry.TryGetOverride(typeof(SampleRequest));

        // Assert
        Assert.Equal("first-rule", match?.RuleName);
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record SampleRequest;

    private sealed record OtherRequest;
#pragma warning restore S2094

    private sealed class SampleBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class OtherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }
}
