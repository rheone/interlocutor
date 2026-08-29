namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using System.Runtime.CompilerServices;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ADR-0011's two behavioral guarantees against Microsoft DI: (1) a singleton that captures the
/// mediator at construction time gets a loud, named exception the moment it tries to dispatch, instead of
/// silently running requests against services resolved from the root provider; (2) dispatch from a real scope
/// shares one scoped dependency instance between the handler and a pipeline behavior, exactly as it would
/// without the guard in place.
/// </summary>
public class RootScopeGuardTests
{
    [Fact]
    public async Task SingletonCapturingMediatorAtConstruction_ThrowsMediatorResolvedFromRootScopeException_OnDispatch()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(RootScopeGuardTests).Assembly));

        // A singleton that injects IMediator in its constructor is exactly the dangerous misuse ADR-0011
        // exists to catch: the mediator instance it captures is resolved once, from the root provider, and
        // held for the life of the process.
        services.AddSingleton<SingletonMediatorConsumer>();

        var provider = services.BuildServiceProvider();
        var consumer = provider.GetRequiredService<SingletonMediatorConsumer>();

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
    public async Task DispatchFromRealScope_SharesOneScopedDependencyInstance_BetweenHandlerAndPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ScopedThing>();
        services.AddScoped<ScopedDependencyRecorder>();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(RootScopeGuardTests).Assembly).Pipeline.AddBehavior(typeof(RecordingScopedBehavior<,>))
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new SharedScopeQuery(), TestContext.Current.CancellationToken);
        var recorder = scope.ServiceProvider.GetRequiredService<ScopedDependencyRecorder>();

        // Assert
        Assert.Equal(2, recorder.RecordedIds.Count);
        Assert.Equal(recorder.RecordedIds[0], recorder.RecordedIds[1]);
        Assert.Equal(recorder.RecordedIds[0], Assert.IsType<IUnion>(result, exactMatch: false).Value);
    }

    /// <summary>A scoped service whose identity (<see cref="Id"/>) proves whether two resolutions within the
    /// same dispatch shared one instance or received two different ones.</summary>
    private sealed class ScopedThing
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>Records the <see cref="ScopedThing.Id"/> observed by each participant in a dispatch - the
    /// pipeline behavior and the handler - so the test can assert they match.</summary>
    private sealed class ScopedDependencyRecorder
    {
        public List<Guid> RecordedIds { get; } = [];
    }

    private sealed record SharedScopeQuery : IQuery<OperationOutcome<Guid, Failure>>;

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class SharedScopeQueryHandler(ScopedThing scopedThing, ScopedDependencyRecorder recorder)
        : IQueryHandler<SharedScopeQuery, OperationOutcome<Guid, Failure>>
    {
        public Task<OperationOutcome<Guid, Failure>> Handle(SharedScopeQuery query, CancellationToken ct)
        {
            recorder.RecordedIds.Add(scopedThing.Id);
            return Task.FromResult<OperationOutcome<Guid, Failure>>(scopedThing.Id);
        }
    }
#pragma warning restore S1144

    private sealed class RecordingScopedBehavior<TRequest, TResponse>(
        ScopedThing scopedThing,
        ScopedDependencyRecorder recorder
    ) : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            recorder.RecordedIds.Add(scopedThing.Id);
            return next();
        }
    }

    /// <summary>A singleton that captures <see cref="IMediator"/> at construction time - the misuse case this
    /// test class exercises. Never legitimate application code; the point is to prove the mediator refuses to
    /// dispatch when reached this way.</summary>
    private sealed class SingletonMediatorConsumer(IMediator mediator)
    {
        public Task<OperationOutcome<Guid, Failure>> DispatchAsync(CancellationToken ct) =>
            mediator.Send(new SharedScopeQuery(), ct);
    }
}
