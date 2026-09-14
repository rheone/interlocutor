namespace Interlocutor.Abstractions.Tests.Pipeline.Diagnostics;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Covers <see cref="PipelineExplainer"/> - the reflective, human-friendly wrapper over
/// <see cref="PipelineChainResolver.ResolveChain{TRequest,TResponse}"/> for a request/response
/// <see cref="Type"/> pair known only at run time. Exercised directly against a manually-built
/// <see cref="IServiceProvider"/>, the same way <c>PipelineChainResolverTests</c> exercises the resolver it
/// wraps, rather than through any <c>AddInterlocutor</c> container translator.
/// </summary>
public class PipelineExplainerTests
{
    [Fact]
    public void Explain_TierTwoBaseline_ReturnsOrderedBehaviorTypesAndNullRuleName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior());
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new BetaBehavior());
        var provider = services.BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act
        var explanation = explainer.Explain(typeof(MarkedRequest), typeof(string), provider);

        // Assert
        Assert.Equal(typeof(MarkedRequest), explanation.RequestType);
        Assert.Equal(typeof(string), explanation.ResponseType);
        Assert.Equal([typeof(AlphaBehavior), typeof(BetaBehavior)], explanation.ResolvedBehaviorTypes);
        Assert.Null(explanation.MatchedOverrideRuleName);
    }

    [Fact]
    public void Explain_TierOneOverrideMatch_ReturnsOverrideOrderAndMatchedRuleName()
    {
        // Arrange - the baseline registers alpha-then-beta; the override rule declares beta-then-alpha.
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior());
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new BetaBehavior());
        services.AddSingleton<IPipelineOverrideRegistry>(
            new FakeOverrideRegistry(t =>
                t == typeof(MarkedRequest)
                    ? new PipelineOverrideMatch([typeof(BetaBehavior), typeof(AlphaBehavior)], "beta-first-rule")
                    : null
            )
        );
        var provider = services.BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act
        var explanation = explainer.Explain(typeof(MarkedRequest), typeof(string), provider);

        // Assert
        Assert.Equal([typeof(BetaBehavior), typeof(AlphaBehavior)], explanation.ResolvedBehaviorTypes);
        Assert.Equal("beta-first-rule", explanation.MatchedOverrideRuleName);
    }

    [Fact]
    public void Explain_NoRegisteredBehaviors_ReturnsEmptyResolvedBehaviorTypes()
    {
        // Arrange - a request/response pair no registered behavior applies to.
        var provider = new ServiceCollection().BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act
        var explanation = explainer.Explain(typeof(MarkedRequest), typeof(string), provider);

        // Assert
        Assert.Empty(explanation.ResolvedBehaviorTypes);
        Assert.Null(explanation.MatchedOverrideRuleName);
    }

    [Fact]
    public void Explain_Disabled_ThrowsInvalidOperationExceptionNamingTheOptionToSet()
    {
        // Arrange
        var provider = new ServiceCollection().BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            explainer.Explain(typeof(MarkedRequest), typeof(string), provider)
        );
        Assert.Contains(nameof(PipelineExplainerOptions.Enabled), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Explain_OpenGenericRequestType_Throws()
    {
        // Arrange
        var provider = new ServiceCollection().BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => explainer.Explain(typeof(OpenGenericRequest<>), typeof(string), provider));
    }

    [Fact]
    public void Explain_OpenGenericResponseType_Throws()
    {
        // Arrange
        var provider = new ServiceCollection().BuildServiceProvider();
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            explainer.Explain(typeof(MarkedRequest), typeof(OpenGenericRequest<>), provider)
        );
    }

    [Fact]
    public void Explain_NullServices_Throws()
    {
        // Arrange
        var explainer = new PipelineExplainer(new PipelineExplainerOptions { Enabled = true });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => explainer.Explain(typeof(MarkedRequest), typeof(string), null!));
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
    // OpenGenericRequest<T> is deliberately never closed over T - only ever referenced via typeof(...) to
    // exercise the open-generic-type-definition rejection.
#pragma warning disable S2094, S2326
    private sealed record MarkedRequest;

    private sealed record OpenGenericRequest<T>;
#pragma warning restore S2094, S2326

    private sealed class AlphaBehavior : IPipelineBehavior<MarkedRequest, string>
    {
        public Task<string> HandleAsync(MarkedRequest request, Func<Task<string>> next, CancellationToken ct) => next();
    }

    private sealed class BetaBehavior : IPipelineBehavior<MarkedRequest, string>
    {
        public Task<string> HandleAsync(MarkedRequest request, Func<Task<string>> next, CancellationToken ct) => next();
    }

    private sealed class FakeOverrideRegistry(Func<Type, PipelineOverrideMatch?> lookup) : IPipelineOverrideRegistry
    {
        public PipelineOverrideMatch? TryGetOverride(Type requestType) => lookup(requestType);
    }
}
