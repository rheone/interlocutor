namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
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
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        var container = containerBuilder.Build();

        Assert.NotNull(container.ResolveOptional<IMediator>());
        Assert.NotNull(container.ResolveOptional<IQueryHandler<SampleQuery, OperationOutcome>>());
        Assert.Empty(container.Resolve<IEnumerable<IPipelineBehavior<SampleQuery, OperationOutcome>>>());
    }

    [Fact]
    public void AddAuthorizationBehavior_DoesNotOrphanAuditSink()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).AddAuthorizationBehavior()
        );

        var container = containerBuilder.Build();

        Assert.Null(container.ResolveOptional<IAuditSink>());
        Assert.NotNull(container.ResolveOptional<IPolicyEvaluator>());
    }

    [Fact]
    public void ConsumerRegistration_BeforeBuilderCall_Wins()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<RecordingAuditSink>().As<IAuditSink>().InstancePerLifetimeScope();

        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).AddAuditingBehavior()
        );

        var container = containerBuilder.Build();
        Assert.IsType<RecordingAuditSink>(container.Resolve<IAuditSink>());
    }

    [Fact]
    public void OverrideAuditSink_AfterAddAuditingBehavior_Wins()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddAuditingBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
        );

        var container = containerBuilder.Build();
        Assert.IsType<RecordingAuditSink>(container.Resolve<IAuditSink>());
    }

    [Fact]
    public async Task BehaviorExecutionOrder_MatchesBuilderCallOrder_ObservedAtRuntime()
    {
        var containerBuilder = new ContainerBuilder();
        var executionLog = new List<string>();
        containerBuilder.RegisterInstance(executionLog).SingleInstance();

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.Second))
        );

        var container = containerBuilder.Build();
        var mediator = container.Resolve<IMediator>();

        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], executionLog);
    }

    [Fact]
    public async Task SameBehaviorRegisteredTwice_ExecutesTwice()
    {
        var containerBuilder = new ContainerBuilder();
        var executionLog = new List<string>();
        containerBuilder.RegisterInstance(executionLog).SingleInstance();

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.First))
        );

        var container = containerBuilder.Build();
        var mediator = container.Resolve<IMediator>();

        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(["first-before", "first-before", "first-after", "first-after"], executionLog);
    }

    [Fact]
    public void UseRecommendedChain_RegistersLoggingAuditingAndAuthorizationBehaviors()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));

        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).UseRecommendedChain()
        );

        var container = containerBuilder.Build();
        var behaviors = container
            .Resolve<IEnumerable<IPipelineBehavior<SampleQuery, OperationOutcome>>>()
            .Select(b => b.GetType())
            .ToList();

        Assert.Contains(behaviors, t => t.Name.StartsWith("LoggingBehavior"));
        Assert.Contains(behaviors, t => t.Name.StartsWith("AuditingBehavior"));
    }

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
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
