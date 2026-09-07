namespace Interlocutor.DependencyInjection.Autofac.Tests;

using System.Runtime.CompilerServices;
using global::Autofac;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ADR-0011's two behavioral guarantees against Autofac: (1) a singleton that captures the mediator at
/// construction time gets a loud, named exception the moment it tries to dispatch, instead of silently
/// running requests against services resolved from the root container; (2) dispatch from a real lifetime
/// scope shares one scoped dependency instance between the handler and a pipeline behavior, exactly as it
/// would without the guard in place. The Microsoft DI counterpart of this test class proves the identical
/// pair of guarantees.
/// </summary>
public class RootScopeGuardTests
{
    [Fact]
    public async Task SingletonCapturingMediatorAtConstruction_ThrowsMediatorResolvedFromRootScopeException_OnDispatch()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(RootScopeGuardTests).Assembly));

        // A singleton that injects IMediator in its constructor is exactly the dangerous misuse ADR-0011
        // exists to catch: the mediator instance it captures is resolved once, from the root container, and
        // held for the life of the process. Autofac's own equivalent leak (instances resolved from the root
        // are rooted there and never disposed) is a further reason this must be caught loudly.
        containerBuilder.RegisterType<SingletonMediatorConsumer>().AsSelf().SingleInstance();

        var container = containerBuilder.Build();
        var consumer = container.Resolve<SingletonMediatorConsumer>();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            consumer.DispatchAsync(TestContext.Current.CancellationToken)
        );

        // Assert
        Assert.Equal(typeof(SharedScopeQuery), ex.RequestType);
        Assert.Contains(nameof(SharedScopeQuery), ex.Message);
        Assert.Contains("scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DispatchFromRealLifetimeScope_SharesOneScopedDependencyInstance_BetweenHandlerAndPipelineBehavior()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<ScopedProbe>().AsSelf().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ScopedDependencyRecorder>().AsSelf().InstancePerLifetimeScope();

        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(RootScopeGuardTests).Assembly).Pipeline.AddBehavior(typeof(RecordingScopedBehavior<,>))
        );

        var container = containerBuilder.Build();
        using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act
        var result = await mediator.SendAsync(new SharedScopeQuery(), TestContext.Current.CancellationToken);

        // Assert
        var recorder = scope.Resolve<ScopedDependencyRecorder>();
        Assert.Equal(2, recorder.RecordedIds.Count);
        Assert.Equal(recorder.RecordedIds[0], recorder.RecordedIds[1]);
        Assert.Equal(recorder.RecordedIds[0], Assert.IsType<IUnion>(result, exactMatch: false).Value);
    }

    /// <summary>A scoped service whose identity (<see cref="Id"/>) proves whether two resolutions within the
    /// same dispatch shared one instance or received two different ones.</summary>
    private sealed class ScopedProbe
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>Records the <see cref="ScopedProbe.Id"/> observed by each participant in a dispatch - the
    /// pipeline behavior and the handler - so the test can assert they match.</summary>
    private sealed class ScopedDependencyRecorder
    {
        public List<Guid> RecordedIds { get; } = [];
    }

    private sealed record SharedScopeQuery : IQuery<Outcome<Guid, Failure>>;

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class SharedScopeQueryHandler(ScopedProbe scopedProbe, ScopedDependencyRecorder recorder)
        : IQueryHandler<SharedScopeQuery, Outcome<Guid, Failure>>
    {
        public Task<Outcome<Guid, Failure>> HandleAsync(SharedScopeQuery query, CancellationToken ct)
        {
            recorder.RecordedIds.Add(scopedProbe.Id);
            return Task.FromResult<Outcome<Guid, Failure>>(scopedProbe.Id);
        }
    }
#pragma warning restore S1144

    private sealed class RecordingScopedBehavior<TRequest, TResponse>(
        ScopedProbe scopedProbe,
        ScopedDependencyRecorder recorder
    ) : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            recorder.RecordedIds.Add(scopedProbe.Id);
            return next();
        }
    }

    /// <summary>A singleton that captures <see cref="IMediator"/> at construction time - the misuse case this
    /// test class exercises. Never legitimate application code; the point is to prove the mediator refuses to
    /// dispatch when reached this way.</summary>
    private sealed class SingletonMediatorConsumer(IMediator mediator)
    {
        public Task<Outcome<Guid, Failure>> DispatchAsync(CancellationToken ct) =>
            mediator.SendAsync(new SharedScopeQuery(), ct);
    }
}
