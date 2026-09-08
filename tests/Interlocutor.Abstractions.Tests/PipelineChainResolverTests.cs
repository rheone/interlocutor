namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Covers <see cref="PipelineChainResolver"/> - the single decision point both dispatch paths consult to
/// enforce a per-request-type pipeline override (see ADR-0019), exercised directly here rather than only
/// indirectly through <see cref="Mediator"/>, so a resolution bug is caught at the layer it actually lives in.
/// </summary>
public class PipelineChainResolverTests
{
    [Fact]
    public void ResolveChain_NoOverrideRegistryRegistered_ReturnsBaselineInRegistrationOrder()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior(log));
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new BetaBehavior(log));
        var provider = services.BuildServiceProvider();

        // Act
        var chain = PipelineChainResolver.ResolveChain<MarkedRequest, string>(provider);

        // Assert
        Assert.Equal([typeof(AlphaBehavior), typeof(BetaBehavior)], chain.Select(b => b.GetType()));
    }

    [Fact]
    public void ResolveChain_RegistryPresentButNoRuleMatches_ReturnsBaselineUnaffected()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior(log));
        services.AddSingleton<IPipelineOverrideRegistry>(new FakeOverrideRegistry(_ => null));
        var provider = services.BuildServiceProvider();

        // Act
        var chain = PipelineChainResolver.ResolveChain<MarkedRequest, string>(provider);

        // Assert
        Assert.Equal([typeof(AlphaBehavior)], chain.Select(b => b.GetType()));
    }

    [Fact]
    public void ResolveChain_MatchedRule_OmitsBehaviorNotNamedInOverride()
    {
        // Arrange - proves the marker-leak-prevention guarantee: a behavior the baseline would otherwise
        // resolve is excluded once an override rule matches, unless the override names it explicitly.
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior(log));
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new BetaBehavior(log));
        services.AddSingleton<IPipelineOverrideRegistry>(
            new FakeOverrideRegistry(t => t == typeof(MarkedRequest) ? [typeof(AlphaBehavior)] : null)
        );
        var provider = services.BuildServiceProvider();

        // Act
        var chain = PipelineChainResolver.ResolveChain<MarkedRequest, string>(provider);

        // Assert
        Assert.Single(chain);
        Assert.IsType<AlphaBehavior>(chain[0]);
    }

    [Fact]
    public void ResolveChain_MatchedRule_PreservesDeclaredOrderNotRegistrationOrder()
    {
        // Arrange - registered alpha-then-beta; the override declares beta-then-alpha.
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new AlphaBehavior(log));
        services.AddSingleton<IPipelineBehavior<MarkedRequest, string>>(new BetaBehavior(log));
        services.AddSingleton<IPipelineOverrideRegistry>(
            new FakeOverrideRegistry(t => t == typeof(MarkedRequest) ? [typeof(BetaBehavior), typeof(AlphaBehavior)] : null)
        );
        var provider = services.BuildServiceProvider();

        // Act
        var chain = PipelineChainResolver.ResolveChain<MarkedRequest, string>(provider);

        // Assert
        Assert.Equal([typeof(BetaBehavior), typeof(AlphaBehavior)], chain.Select(b => b.GetType()));
    }

    [Fact]
    public void ResolveChain_UnrelatedRequestType_IsUnaffectedByAnotherTypesOverride()
    {
        // Arrange - a rule matching only MarkedRequest must not affect a different request type dispatched
        // through the same registry.
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<string, string>>(new NamedStringBehavior("unrelated", log));
        services.AddSingleton<IPipelineOverrideRegistry>(
            new FakeOverrideRegistry(t => t == typeof(MarkedRequest) ? [typeof(AlphaBehavior)] : null)
        );
        var provider = services.BuildServiceProvider();

        // Act
        var chain = PipelineChainResolver.ResolveChain<string, string>(provider);

        // Assert
        Assert.Equal([typeof(NamedStringBehavior)], chain.Select(b => b.GetType()));
    }

    // Deliberately empty: a type-identity marker for this test's generic-constraint shape, not a data holder.
#pragma warning disable S2094
    private sealed record MarkedRequest;
#pragma warning restore S2094

    private sealed class FakeOverrideRegistry(Func<Type, IReadOnlyList<Type>?> lookup) : IPipelineOverrideRegistry
    {
        public PipelineOverrideMatch? TryGetOverride(Type requestType)
        {
            var found = lookup(requestType);
            return found is null ? null : new PipelineOverrideMatch(found, "fake-rule");
        }
    }

    private sealed class AlphaBehavior(List<string> log) : IPipelineBehavior<MarkedRequest, string>
    {
        public Task<string> HandleAsync(MarkedRequest request, Func<Task<string>> next, CancellationToken ct)
        {
            log.Add("alpha");
            return next();
        }
    }

    private sealed class BetaBehavior(List<string> log) : IPipelineBehavior<MarkedRequest, string>
    {
        public Task<string> HandleAsync(MarkedRequest request, Func<Task<string>> next, CancellationToken ct)
        {
            log.Add("beta");
            return next();
        }
    }

    private sealed class NamedStringBehavior(string name, List<string> log) : IPipelineBehavior<string, string>
    {
        public Task<string> HandleAsync(string request, Func<Task<string>> next, CancellationToken ct)
        {
            log.Add(name);
            return next();
        }
    }
}
