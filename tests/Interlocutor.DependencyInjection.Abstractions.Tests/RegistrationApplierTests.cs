namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

/// <summary>
/// Covers <see cref="RegistrationApplier"/> directly, independent of either container package: the
/// seven-step order it drives an <see cref="InterlocutorBuilder"/>'s accumulated registrations through, and
/// that it forwards each registration's <see cref="RegistrationLifetime"/> to its callback unchanged
/// regardless of which lifetime it is.
/// </summary>
public class RegistrationApplierTests
{
    [Fact]
    public void Apply_DrivesEveryStep_InTheSevenStepOrder()
    {
        // Arrange
        var builder = new InterlocutorBuilder();
        builder
            .AddHandler(typeof(MarkerHandler))
            .AddGeneratedRegistrations([
                new InterlocutorRegistration(
                    typeof(IQueryHandler<MarkerGeneratedQuery, OperationOutcome>),
                    typeof(MarkerGeneratedHandler),
                    RegistrationLifetime.Scoped,
                    RegistrationKind.Handler
                ),
            ])
            // Scanning is required for AddTypeMatch's candidates - the whole test assembly, since
            // CandidateTypesForMatching has no narrower opt-in - but the predicate below narrows the match
            // itself to exactly one marker type, so this step's own recorded index stays unambiguous even
            // though the Handlers step above also picks up (and correctly ignores, for this test's purposes)
            // every other handler the scan finds.
            .ScanAssembly(typeof(RegistrationApplierTests).Assembly)
            .AddTypeMatch(t => t == typeof(MarkerMatchedHandler), TypeMatchTarget.Handler);
        builder
            .Pipeline.AddStage<MarkerStage>()
            .AddStage<MarkerStageFactoryRequest, MarkerStageFactoryResponse>(
                onBefore: (Func<MarkerStageFactoryRequest, CancellationToken, Task<StageResult<MarkerStageFactoryResponse>>>?)
                    null,
                onAfter: null
            )
            .AddBehavior(typeof(MarkerBehavior<,>));

        var steps = new List<(string Category, Type MarkerType)>();
        var applier = new RegistrationApplier(
            addRegistration: r => steps.Add(("registration", r.ImplementationType)),
            addFactory: f => steps.Add(("factory", f.ServiceType)),
            tryAddDefault: d => steps.Add(("default", d.ImplementationType))
        );

        // Act
        applier.Apply(builder);

        // Assert
        int IndexOf(string category, Type markerType) =>
            steps.FindIndex(s => s.Category == category && s.MarkerType == markerType);

        var handlerIndex = IndexOf("registration", typeof(MarkerHandler));
        var generatedIndex = IndexOf("registration", typeof(MarkerGeneratedHandler));
        var stageIndex = IndexOf("registration", typeof(MarkerStage));
        var stageFactoryIndex = IndexOf(
            "factory",
            typeof(IPipelineStage<MarkerStageFactoryRequest, MarkerStageFactoryResponse>)
        );
        var behaviorIndex = IndexOf("registration", typeof(MarkerBehavior<,>));

        // MarkerMatchedHandler is discovered twice: once by ordinary assembly scanning (it implements a
        // handler contract, so HandlerDiscovery.Discover finds it as part of the Handlers step like any other
        // scanned handler - Handlers and MatchedRegistrations are independent, non-deduplicated lists by
        // design) and once by the type-matching rule itself, in the MatchedRegistrations step. The step under
        // test here is the latter, always the last occurrence given the seven-step order.
        var matchedIndex = steps.FindLastIndex(s =>
            s.Category == "registration" && s.MarkerType == typeof(MarkerMatchedHandler)
        );
        var defaultIndex = IndexOf("default", typeof(AmbientRequestContextAccessor));

        Assert.True(handlerIndex >= 0, "Handler marker was not applied.");
        Assert.True(generatedIndex >= 0, "Generated-registration marker was not applied.");
        Assert.True(stageIndex >= 0, "Stage marker was not applied.");
        Assert.True(stageFactoryIndex >= 0, "Stage-factory marker was not applied.");
        Assert.True(behaviorIndex >= 0, "Behavior marker was not applied.");
        Assert.True(matchedIndex >= 0, "Matched-registration marker was not applied.");
        Assert.True(defaultIndex >= 0, "Default marker was not applied.");

        Assert.True(handlerIndex < generatedIndex, "Handlers must be applied before generated registrations.");
        Assert.True(generatedIndex < stageIndex, "Generated registrations must be applied before stages.");
        Assert.True(stageIndex < stageFactoryIndex, "Stages must be applied before stage factories.");
        Assert.True(stageFactoryIndex < behaviorIndex, "Stage factories must be applied before behaviors.");
        Assert.True(behaviorIndex < matchedIndex, "Behaviors must be applied before matched registrations.");
        Assert.True(matchedIndex < defaultIndex, "Matched registrations must be applied before defaults.");
    }

    [Theory]
    [InlineData(RegistrationLifetime.Singleton)]
    [InlineData(RegistrationLifetime.Scoped)]
    [InlineData(RegistrationLifetime.Transient)]
    public void Apply_ForwardsEachRegistrationsLifetime_Unchanged(RegistrationLifetime lifetime)
    {
        // Arrange
        var builder = new InterlocutorBuilder();
        builder.AddHandler(typeof(MarkerHandler), lifetime);

        RegistrationLifetime? observed = null;
        var applier = new RegistrationApplier(
            addRegistration: r =>
            {
                if (r.ImplementationType == typeof(MarkerHandler))
                {
                    observed = r.Lifetime;
                }
            },
            addFactory: _ => { },
            tryAddDefault: _ => { }
        );

        // Act
        applier.Apply(builder);

        // Assert
        Assert.Equal(lifetime, observed);
    }

    [Theory]
    [InlineData(RegistrationLifetime.Singleton)]
    [InlineData(RegistrationLifetime.Scoped)]
    [InlineData(RegistrationLifetime.Transient)]
    public void Apply_ForwardsEachFactoryRegistrationsLifetime_Unchanged(RegistrationLifetime lifetime)
    {
        // Arrange
        var builder = new InterlocutorBuilder();
        builder.Pipeline.AddStage<MarkerStageFactoryRequest, MarkerStageFactoryResponse>(
            onBefore: (Func<MarkerStageFactoryRequest, CancellationToken, Task<StageResult<MarkerStageFactoryResponse>>>?)null,
            onAfter: null,
            lifetime
        );

        RegistrationLifetime? observed = null;
        var applier = new RegistrationApplier(
            addRegistration: _ => { },
            addFactory: f => observed = f.Lifetime,
            tryAddDefault: _ => { }
        );

        // Act
        applier.Apply(builder);

        // Assert
        Assert.Equal(lifetime, observed);
    }

    private sealed class MarkerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed record MarkerStageFactoryRequest;

    private sealed record MarkerStageFactoryResponse;

    private sealed class MarkerStage : IPipelineStage<MarkerStageFactoryRequest, MarkerStageFactoryResponse>
    {
        public Task<StageResult<MarkerStageFactoryResponse>> OnBeforeAsync(
            MarkerStageFactoryRequest request,
            CancellationToken ct
        ) => Task.FromResult(StageResult<MarkerStageFactoryResponse>.Continue);

        public Task OnAfterAsync(MarkerStageFactoryRequest request, MarkerStageFactoryResponse result, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed record MarkerQuery : IQuery<OperationOutcome>;

    private sealed class MarkerHandler : IQueryHandler<MarkerQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(MarkerQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed record MarkerGeneratedQuery : IQuery<OperationOutcome>;

    private sealed class MarkerGeneratedHandler : IQueryHandler<MarkerGeneratedQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(MarkerGeneratedQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed record MarkerMatchedQuery : IQuery<OperationOutcome>;

    private sealed class MarkerMatchedHandler : IQueryHandler<MarkerMatchedQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(MarkerMatchedQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }
}
