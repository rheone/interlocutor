namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.DependencyInjection.Abstractions;

public class InterlocutorBuilderIntegrationTests
{
    [Fact]
    public void BareBuilder_RegistersMediatorAndHandlers_ButZeroBehaviors()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        Assert.Contains(services, s => s.ServiceType == typeof(IMediator));
        Assert.Contains(services, s => s.ServiceType == typeof(IQueryHandler<SampleQuery, OperationOutcome>));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddAuditingBehavior_RegistersAuditSinkDefault_ButAuthorizationBehaviorDoesNotOrphanIt()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).AddAuthorizationBehavior()
        );

        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IAuditSink));
        Assert.Contains(services, s => s.ServiceType == typeof(IPolicyEvaluator));
    }

    [Fact]
    public void ConsumerRegistration_BeforeBuilderCall_Wins()
    {
        var services = new ServiceCollection();
        services.AddScoped<IAuditSink, RecordingAuditSink>();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<IAuditSink>();
        Assert.IsType<RecordingAuditSink>(sink);
    }

    [Fact]
    public void OverrideAuditSink_AfterAddAuditingBehavior_Wins()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddAuditingBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
        );

        var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<IAuditSink>();
        Assert.IsType<RecordingAuditSink>(sink);
    }

    [Fact]
    public async Task BehaviorExecutionOrder_MatchesBuilderCallOrder_ObservedAtRuntime()
    {
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.Second))
        );

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], executionLog);
    }

    [Fact]
    public async Task SameBehaviorRegisteredTwice_ExecutesTwice()
    {
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.First))
        );

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(["first-before", "first-before", "first-after", "first-after"], executionLog);
    }

    [Fact]
    public void UseRecommendedChain_RegistersLoggingAuditingAndAuthorizationBehaviors()
    {
        var services = new ServiceCollection();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).UseRecommendedChain()
        );

        var behaviorImplementationTypes = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(s => s.ImplementationType)
            .ToList();

        Assert.Contains(typeof(LoggingBehavior<,>), behaviorImplementationTypes);
        Assert.Contains(typeof(AuditingBehavior<,>), behaviorImplementationTypes);
        Assert.Contains(typeof(AuthorizationBehavior<,>), behaviorImplementationTypes);
    }

    [Fact]
    public async Task StageHost_WithTypeRegisteredStage_RunsBeforeAndAfterAroundTheHandler()
    {
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).AddStage<RecordingStage>().AddStageHost()
        );

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(default, result);
        Assert.Equal(["stage-before", "stage-after"], executionLog);
    }

    [Fact]
    public async Task StageHost_WithShortCircuitingDelegateStage_SkipsHandlerAndReturnsShortCircuitResponseUnchanged()
    {
        var services = new ServiceCollection();
        var handlerRan = false;
        services.AddSingleton(new HandlerRanFlag(() => handlerRan = true));
        services.AddLogging();

        var declined = new OperationOutcome(new Interlocutor.CommonKernel.ResultTypes.Failure("declined by stage"));

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddStage<ShortCircuitQuery, OperationOutcome>(
                    (request, ct) => Task.FromResult(StageResult<OperationOutcome>.ShortCircuit(declined)),
                    null
                )
                .AddStageHost()
        );

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act: the response flows out of the mediator exactly as a handled response would --
        // nothing observing it from outside the stage host can tell it was short-circuited.
        var result = await mediator.Send(new ShortCircuitQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(declined, result);
        Assert.False(handlerRan);
    }

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class RecordingStage(List<string> log) : PipelineStageBase<SampleQuery, OperationOutcome>
    {
        public override async Task<StageResult<OperationOutcome>> OnBeforeAsync(SampleQuery request, CancellationToken ct)
        {
            log.Add("stage-before");
            await Task.CompletedTask;
            return StageResult<OperationOutcome>.Continue;
        }

        public override async Task OnAfterAsync(SampleQuery request, OperationOutcome result, CancellationToken ct)
        {
            log.Add("stage-after");
            await Task.CompletedTask;
        }
    }

    private sealed record ShortCircuitQuery : IQuery<OperationOutcome>;

    private sealed class HandlerRanFlag(Action onHandled)
    {
        public void MarkHandled() => onHandled();
    }

    private sealed class ShortCircuitQueryHandler(HandlerRanFlag flag) : IQueryHandler<ShortCircuitQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(ShortCircuitQuery query, CancellationToken ct)
        {
            flag.MarkHandled();
            return Task.FromResult(default(OperationOutcome));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public Task RecordAsync(AuditEntry entry, CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Container for two distinct recording behaviors so both can share the log-entry naming convention.</summary>
    private static class RecordingBehavior<TRequest, TResponse>
    {
        public sealed class First(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        {
            public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
            {
                log.Add("first-before");
                var result = await next();
                log.Add("first-after");
                return result;
            }
        }

        public sealed class Second(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        {
            public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
            {
                log.Add("second-before");
                var result = await next();
                log.Add("second-after");
                return result;
            }
        }
    }
}
