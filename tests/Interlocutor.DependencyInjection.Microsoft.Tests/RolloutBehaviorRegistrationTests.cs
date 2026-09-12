namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using global::Microsoft.Extensions.Options;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Covers <c>InterlocutorPipelineBuilder.AddRolloutBehavior</c> under Microsoft DI - the shipped
/// percentage-rollout gate bound to a named <see cref="IOptionsMonitor{TOptions}"/> registration, including
/// the live-reload capability this container package adds on top of the container-neutral
/// <c>AddConditionalBehavior</c>.
/// </summary>
public class RolloutBehaviorRegistrationTests
{
    [Fact]
    public async Task AddRolloutBehavior_ZeroPercent_NeverRunsTheGatedBehavior()
    {
        // Arrange
        var services = NewServices();
        services.Configure<RolloutOptions>("Canary", o => o.PercentageEnabled = 0);
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>("Canary")
        );
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, TestResponse>>();
        var counter = provider.GetRequiredService<InvocationCounter>();

        // Act - a large sample makes a flaky true-by-chance result effectively impossible.
        for (var i = 0; i < 200; i++)
        {
            await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        }

        // Assert
        Assert.Equal(0, counter.Count);
    }

    [Fact]
    public async Task AddRolloutBehavior_HundredPercent_AlwaysRunsTheGatedBehavior()
    {
        // Arrange
        var services = NewServices();
        services.Configure<RolloutOptions>("Canary", o => o.PercentageEnabled = 100);
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>("Canary")
        );
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, TestResponse>>();
        var counter = provider.GetRequiredService<InvocationCounter>();

        // Act
        for (var i = 0; i < 200; i++)
        {
            await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        }

        // Assert
        Assert.Equal(200, counter.Count);
    }

    [Fact]
    public async Task AddRolloutBehavior_NamedOptionsReloaded_NextEvaluationReflectsTheNewPercentage_WithoutRebuildingTheContainer()
    {
        // Arrange - a mutable closure captured by the named Configure delegate stands in for a live
        // configuration source: evicting the cached IOptionsMonitor entry (what a real IConfiguration change
        // token firing does internally) forces the next Get() to re-run this delegate and pick up the new
        // value, proving the gate reads live rather than a value snapshotted once at startup.
        var currentPercentage = 0.0;
        var services = NewServices();
        services.Configure<RolloutOptions>("ReloadCanary", o => o.PercentageEnabled = currentPercentage);
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>("ReloadCanary")
        );
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, TestResponse>>();
        var counter = provider.GetRequiredService<InvocationCounter>();

        // Act & Assert - still 0% before any reload.
        await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        Assert.Equal(0, counter.Count);

        // Act - simulate a live configuration reload to 100%, without touching the container or the builder.
        currentPercentage = 100;
        provider.GetRequiredService<IOptionsMonitorCache<RolloutOptions>>().TryRemove("ReloadCanary");

        // Assert - the very next evaluation already reflects the new value.
        for (var i = 0; i < 50; i++)
        {
            await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        }

        Assert.Equal(50, counter.Count);
    }

    [Fact]
    public async Task AddRolloutBehavior_TwoNamedCanaries_RunConcurrentlyWithIndependentPercentages()
    {
        // Arrange - proves the named-options split: two canaries configured under different names don't
        // share a percentage.
        var services = NewServices();
        services.Configure<RolloutOptions>("NeverCanary", o => o.PercentageEnabled = 0);
        services.Configure<RolloutOptions>("AlwaysCanary", o => o.PercentageEnabled = 100);
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>("NeverCanary")
                .AddRolloutBehavior<OtherTestBehavior, OtherTestRequest, OtherTestResponse>("AlwaysCanary")
        );
        var provider = services.BuildServiceProvider();
        var neverBehavior = provider.GetRequiredService<IPipelineBehavior<TestRequest, TestResponse>>();
        var alwaysBehavior = provider.GetRequiredService<IPipelineBehavior<OtherTestRequest, OtherTestResponse>>();
        var counter = provider.GetRequiredService<InvocationCounter>();

        // Act
        await neverBehavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        await alwaysBehavior.HandleAsync(
            new OtherTestRequest(),
            () => Task.FromResult(new OtherTestResponse()),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(1, counter.Count);
    }

    /// <summary>
    /// A fresh <see cref="ServiceCollection"/> with a null <see cref="ILogger{TCategoryName}"/> registered,
    /// mirroring <c>BehaviorSubsetConstraintTests.NewServices</c>, plus a shared <see cref="InvocationCounter"/>
    /// singleton the gated test behaviors below record into - constructor-injected rather than static, so
    /// state never leaks across tests even under parallel test execution.
    /// </summary>
    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<InvocationCounter>();
        return services;
    }

    /// <summary>How many times a gated test behavior actually ran, shared across both test behaviors below.</summary>
    private sealed class InvocationCounter
    {
        public int Count { get; set; }
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record TestRequest;

    private sealed record TestResponse;

    private sealed record OtherTestRequest;

    private sealed record OtherTestResponse;
#pragma warning restore S2094

    private sealed class TestBehavior(InvocationCounter counter) : IPipelineBehavior<TestRequest, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequest request, Func<Task<TestResponse>> next, CancellationToken ct)
        {
            counter.Count++;
            return Task.FromResult(new TestResponse());
        }
    }

    /// <summary>A second, independent gated behavior for the concurrent-canaries test.</summary>
    private sealed class OtherTestBehavior(InvocationCounter counter) : IPipelineBehavior<OtherTestRequest, OtherTestResponse>
    {
        public Task<OtherTestResponse> HandleAsync(
            OtherTestRequest request,
            Func<Task<OtherTestResponse>> next,
            CancellationToken ct
        )
        {
            counter.Count++;
            return Task.FromResult(new OtherTestResponse());
        }
    }
}
