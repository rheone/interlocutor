namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Abstractions;

public class InterlocutorBuilderIntegrationTests
{
    [Fact]
    public void BareBuilder_RegistersMediatorAndHandlers_ButZeroBehaviors()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        // Act
        var container = containerBuilder.Build();

        // Assert
        Assert.NotNull(container.ResolveOptional<IMediator>());
        Assert.NotNull(container.ResolveOptional<IQueryHandler<SampleQuery, OperationOutcome>>());
        Assert.Empty(container.Resolve<IEnumerable<IPipelineBehavior<SampleQuery, OperationOutcome>>>());
    }

    [Fact]
    public void AddInterlocutor_WithValidateOnBuild_ThrowsAggregatingEveryProblem_WhenTheConfigurationHasAny()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();

        // Act
        var exception = Assert.Throws<InterlocutorRegistrationValidationException>(() =>
            containerBuilder.AddInterlocutor(builder =>
                builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).ValidateOnBuild()
            )
        );

        // Assert
        Assert.Contains(
            exception.Report.Problems,
            p =>
                p.Kind == RegistrationProblemKind.UnhandledRequestType
                && p.Description.Contains(nameof(UnhandledValidationQuery))
        );
    }

    [Fact]
    public void AddInterlocutor_WithoutValidateOnBuild_DoesNotThrow_EvenWithAnUnhandledRequestType()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();

        // Act
        var exception = Record.Exception(() =>
            containerBuilder.AddInterlocutor(builder =>
                builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
            )
        );

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AddInterlocutor_WithLoggerFactoryRegistered_LogsTheRegistrationReport()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        using var recordingFactory = new RecordingLoggerFactory();
        containerBuilder.RegisterInstance<ILoggerFactory>(recordingFactory).ExternallyOwned();
        containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        // Act
        using var container = containerBuilder.Build();

        // Assert
        var logger = Assert.Single(recordingFactory.CreatedLoggers);
        var message = Assert.Single(logger.Messages);
        Assert.Contains("Interlocutor registration report", message);
    }

    [Fact]
    public void AddInterlocutor_WithoutLoggerFactoryRegistered_DoesNotThrow()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        // Act
        // Built just to prove Build() doesn't throw - not kept around, so never disposed; the same
        // inline-container idiom .editorconfig's IDISP001 override documents for this test suite.
#pragma warning disable IDISP005
        var exception = Record.Exception(() => containerBuilder.Build());
#pragma warning restore IDISP005

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AddAuthorizationBehavior_DoesNotOrphanAuditSink()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuthorizationBehavior()
        );

        // Act
        var container = containerBuilder.Build();

        // Assert
        Assert.Null(container.ResolveOptional<IAuditSink>());
        Assert.NotNull(container.ResolveOptional<IPolicyEvaluator>());
    }

    [Fact]
    public void ConsumerRegistration_BeforeBuilderCall_Wins()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<RecordingAuditSink>().As<IAuditSink>().InstancePerLifetimeScope();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuditingBehavior()
        );

        // Act
        var container = containerBuilder.Build();

        // Assert
        Assert.IsType<RecordingAuditSink>(container.Resolve<IAuditSink>());
    }

    [Fact]
    public void OverrideAuditSink_AfterAddAuditingBehavior_Wins()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddAuditingBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
        );

        // Act
        var container = containerBuilder.Build();

        // Assert
        Assert.IsType<RecordingAuditSink>(container.Resolve<IAuditSink>());
    }

    [Fact]
    public async Task BehaviorExecutionOrder_MatchesBuilderCallOrder_ObservedAtRuntime()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var executionLog = new List<string>();
        containerBuilder.RegisterInstance(executionLog).SingleInstance();

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.Second))
        );

        var container = containerBuilder.Build();
        // Dispatch must happen from a real scope, never the root container directly - see ADR-0011.
        using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act
        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], executionLog);
    }

    [Fact]
    public async Task SameBehaviorRegisteredTwice_ExecutesTwice()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var executionLog = new List<string>();
        containerBuilder.RegisterInstance(executionLog).SingleInstance();

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.First))
        );

        var container = containerBuilder.Build();
        // Dispatch must happen from a real scope, never the root container directly - see ADR-0011.
        using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act
        await mediator.Send(new SampleQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first-before", "first-before", "first-after", "first-after"], executionLog);
    }

    [Fact]
    public void UseRecommendedChain_RegistersLoggingAuditingAndAuthorizationBehaviors()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));

        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.UseRecommendedChain()
        );

        var container = containerBuilder.Build();

        // Act
        // AuthorizationBehavior<TRequest,TResponse> only ever short-circuits meaningfully for a response
        // shaped OperationOutcome<TSuccess, Forbidden> (it needs a Forbidden case to construct on denial), so
        // it's resolved against AuthorizableQuery (not the plain-OperationOutcome SampleQuery every other test
        // here uses) to genuinely exercise closed-generic resolution rather than only the open-generic
        // registration list. AuthorizableQuery also implements IAuditableRequest and IRequiresPolicy so that,
        // now that AuditingBehavior/AuthorizationBehavior constrain TRequest to those markers, this query still
        // draws all three behaviors from the recommended chain instead of being filtered down to just logging.
        var behaviors = container
            .Resolve<IEnumerable<IPipelineBehavior<AuthorizableQuery, OperationOutcome<string, Forbidden>>>>()
            .Select(b => b.GetType())
            .ToList();

        // Assert
        Assert.Contains(behaviors, t => t.Name.StartsWith("LoggingBehavior"));
        Assert.Contains(behaviors, t => t.Name.StartsWith("AuditingBehavior"));
        Assert.Contains(behaviors, t => t.Name.StartsWith("AuthorizationBehavior"));
    }

    [Fact]
    public async Task StageHost_WithShortCircuitingDelegateStage_SkipsHandlerAndReturnsShortCircuitResponseUnchanged()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));

        var handlerRan = false;
        containerBuilder.RegisterInstance(new HandlerRanFlag(() => handlerRan = true)).SingleInstance();

        var declined = new OperationOutcome(new Failure("declined by stage"));

        containerBuilder.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddStage<ShortCircuitQuery, OperationOutcome>(
                    (_, _) => Task.FromResult(StageResult<OperationOutcome>.ShortCircuit(declined)),
                    null
                )
                .AddStageHost()
        );

        var container = containerBuilder.Build();
        // Dispatch must happen from a real scope, never the root container directly - see ADR-0011.
        using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act: the response flows out of the mediator exactly as a handled response would -- nothing
        // observing it from outside the stage host can tell it was short-circuited.
        var result = await mediator.Send(new ShortCircuitQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(declined, result);
        Assert.False(handlerRan);
    }

    [Fact]
    public void AddCancellationBehavior_ResolvesWithoutThrowing_UnderAutofac()
    {
        // The exact open-generic-resolution failure mode documented on AuthorizationBehavior's remarks and
        // fixed once before (audit-followups/13): a behavior whose constrained TRequest/TResponse shape
        // throws from Autofac's open-generic substitution instead of simply resolving. Registering and
        // resolving against a closed generic that satisfies the constraints is the only way to prove this.

        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddCancellationBehavior()
        );

        var container = containerBuilder.Build();
        using var scope = container.BeginLifetimeScope();

        // Act
        var behaviors = scope
            .Resolve<IEnumerable<IPipelineBehavior<CancellableQuery, OperationOutcome<string, Cancelled>>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("CancellationBehavior"));
    }

    [Fact]
    public void AddTimeoutBehavior_ResolvesWithoutThrowing_UnderAutofac()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddTimeoutBehavior()
        );

        var container = containerBuilder.Build();
        using var scope = container.BeginLifetimeScope();

        // Act
        var behaviors = scope
            .Resolve<IEnumerable<IPipelineBehavior<TimeBoundQuery, OperationOutcome<string, Timeout>>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("TimeoutBehavior"));
    }

    [Fact]
    public void AddRateLimitedBehavior_ResolvesWithoutThrowing_UnderAutofac()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddRateLimitedBehavior()
        );

        var container = containerBuilder.Build();
        using var scope = container.BeginLifetimeScope();

        // Act
        var behaviors = scope
            .Resolve<IEnumerable<IPipelineBehavior<ThrottledQuery, OperationOutcome<string, RateLimited>>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("RateLimitedBehavior"));
    }

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    // The handler classes through ShortCircuitQueryHandler below are resolved only via
    // AddInterlocutor()'s assembly-scanning DI registration - never new'd or typeof'd directly in
    // this file - so their constructors are invisible to static analysis and read as unused.
#pragma warning disable S1144
    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    /// <summary>Deliberately has no handler, so ValidateOnBuild() tests have a deterministic problem to find.</summary>
    private sealed record UnhandledValidationQuery : IQuery<OperationOutcome>;

    private sealed record CancellableQuery : IQuery<OperationOutcome<string, Cancelled>>, ICancellable;

    private sealed class CancellableQueryHandler : IQueryHandler<CancellableQuery, OperationOutcome<string, Cancelled>>
    {
        public Task<OperationOutcome<string, Cancelled>> Handle(CancellableQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<string, Cancelled>>("ok");
    }

    private sealed record TimeBoundQuery : IQuery<OperationOutcome<string, Timeout>>, IHasTimeout
    {
        public TimeSpan Budget => TimeSpan.FromSeconds(30);
    }

    private sealed class TimeBoundQueryHandler : IQueryHandler<TimeBoundQuery, OperationOutcome<string, Timeout>>
    {
        public Task<OperationOutcome<string, Timeout>> Handle(TimeBoundQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<string, Timeout>>("ok");
    }

    private sealed record ThrottledQuery : IQuery<OperationOutcome<string, RateLimited>>, IRateLimited
    {
        public string LimitKey => "test-limit";
    }

    private sealed class ThrottledQueryHandler : IQueryHandler<ThrottledQuery, OperationOutcome<string, RateLimited>>
    {
        public Task<OperationOutcome<string, RateLimited>> Handle(ThrottledQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<string, RateLimited>>("ok");
    }

    private sealed record AuthorizableQuery : IQuery<OperationOutcome<string, Forbidden>>, IAuditableRequest, IRequiresPolicy
    {
        public string ActionName => "Query.Authorizable";

        public string PolicyName => "CanRead";
    }

    private sealed class AuthorizableQueryHandler : IQueryHandler<AuthorizableQuery, OperationOutcome<string, Forbidden>>
    {
        public Task<OperationOutcome<string, Forbidden>> Handle(AuthorizableQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<string, Forbidden>>("ok");
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
#pragma warning restore S1144

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
                var result = await next().ConfigureAwait(false);
                log.Add("first-after");
                return result;
            }
        }

        public sealed class Second(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        {
            public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
            {
                log.Add("second-before");
                var result = await next().ConfigureAwait(false);
                log.Add("second-after");
                return result;
            }
        }
    }

    /// <summary>An <see cref="ILoggerFactory"/> whose loggers record every formatted message they receive.</summary>
    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public List<RecordingLogger> CreatedLoggers { get; } = [];

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new RecordingLogger();
            this.CreatedLoggers.Add(logger);
            return logger;
        }

        public void Dispose() { }
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => this.Messages.Add(formatter(state, exception));
    }
}
