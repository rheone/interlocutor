namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Covers <c>InterlocutorPipelineBuilder.AddRolloutBehavior</c> under Autofac - the shipped
/// percentage-rollout gate evaluated against a fixed <see cref="RolloutOptions"/> snapshot, since Autofac has
/// no <c>IOptionsMonitor</c>-equivalent live-reload mechanism the way Microsoft DI does.
/// </summary>
public class RolloutBehaviorRegistrationTests
{
    [Fact]
    public async Task AddRolloutBehavior_ZeroPercent_NeverRunsTheGatedBehavior()
    {
        // Arrange
        var counter = new InvocationCounter();
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(counter);
        containerBuilder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>(
                    new RolloutOptions { PercentageEnabled = 0 }
                )
        );
        var container = containerBuilder.Build();
        var behavior = container.Resolve<IPipelineBehavior<TestRequest, TestResponse>>();

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
        var counter = new InvocationCounter();
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(counter);
        containerBuilder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>(
                    new RolloutOptions { PercentageEnabled = 100 }
                )
        );
        var container = containerBuilder.Build();
        var behavior = container.Resolve<IPipelineBehavior<TestRequest, TestResponse>>();

        // Act
        for (var i = 0; i < 200; i++)
        {
            await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        }

        // Assert
        Assert.Equal(200, counter.Count);
    }

    [Fact]
    public async Task AddRolloutBehavior_OptionsSnapshotIsFixed_MutatingTheOriginalInstanceAfterwardHasNoEffect()
    {
        // Arrange - Autofac's substitute for live reload is documented as a fixed snapshot: mutating the same
        // RolloutOptions instance after the container is built must not change already-resolved behavior,
        // proving the value was captured, not re-read from a still-referenced options object.
        var counter = new InvocationCounter();
        var options = new RolloutOptions { PercentageEnabled = 0 };
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(counter);
        containerBuilder.AddInterlocutor(cfg =>
            cfg.ScanAssembly(typeof(RolloutBehaviorRegistrationTests).Assembly)
                .Pipeline.AddRolloutBehavior<TestBehavior, TestRequest, TestResponse>(options)
        );
        var container = containerBuilder.Build();
        var behavior = container.Resolve<IPipelineBehavior<TestRequest, TestResponse>>();

        // Act - mutate the original instance after the container already captured the gate's closure.
        options.PercentageEnabled = 100;
        for (var i = 0; i < 50; i++)
        {
            await behavior.HandleAsync(new TestRequest(), () => Task.FromResult(new TestResponse()), CancellationToken.None);
        }

        // Assert - this documents the capability difference from Microsoft DI's live reload rather than
        // asserting a specific implementation detail: whichever way it resolves, it must be consistent, not
        // sometimes-reload. A snapshot taken at configuration time stays at 0%.
        Assert.Equal(0, counter.Count);
    }

    private sealed class InvocationCounter
    {
        public int Count { get; set; }
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record TestRequest;

    private sealed record TestResponse;
#pragma warning restore S2094

    private sealed class TestBehavior(InvocationCounter counter) : IPipelineBehavior<TestRequest, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestRequest request, Func<Task<TestResponse>> next, CancellationToken ct)
        {
            counter.Count++;
            return Task.FromResult(new TestResponse());
        }
    }
}
