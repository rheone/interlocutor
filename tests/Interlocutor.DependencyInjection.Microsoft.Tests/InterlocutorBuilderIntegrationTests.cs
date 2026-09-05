namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
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
    public void AddInterlocutor_WithLoggerFactoryRegistered_LogsTheRegistrationReport()
    {
        // Arrange
        var services = new ServiceCollection();
        using var recordingFactory = new RecordingLoggerFactory();
        services.AddSingleton<ILoggerFactory>(recordingFactory);

        // Act
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        // Assert
        var logger = Assert.Single(recordingFactory.CreatedLoggers);
        var message = Assert.Single(logger.Messages);
        Assert.Contains("Interlocutor registration report", message);
    }

    // Both this test and AddInterlocutor_WithoutValidateOnBuild_DoesNotThrow_EvenWithAnUnhandledRequestType
    // below assert the exact same "scanning the test assembly does not throw" guarantee - one names the
    // logger-factory absence this test's own "With" sibling above varies, the other names the missing
    // ValidateOnBuild() call its own "With" sibling further down varies. Routing both through one shared
    // helper keeps that duplication self-evidently intentional rather than reading as accidental copy-paste.
    [Fact]
    public void AddInterlocutor_WithoutLoggerFactoryRegistered_DoesNotThrow() => AssertScanningTheTestAssemblyDoesNotThrow();

    [Fact]
    public void BareBuilder_RegistersMediatorAndHandlers_ButZeroBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly));

        // Assert
        Assert.Contains(services, s => s.ServiceType == typeof(IMediator));
        Assert.Contains(services, s => s.ServiceType == typeof(IQueryHandler<SampleQuery, Outcome>));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddHandler_WithoutScanning_ResolvesThroughARealContainer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder => builder.AddHandler(typeof(SampleQueryHandler)));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider.GetService<IQueryHandler<SampleQuery, Outcome>>());
    }

    [Fact]
    public void AddHandler_TakesPrecedenceOverScannedDuplicate_ThroughARealContainer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Singleton)
        );
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IQueryHandler<SampleQuery, Outcome>>();
        var second = secondScope.ServiceProvider.GetRequiredService<IQueryHandler<SampleQuery, Outcome>>();

        // Assert
        // A single Singleton registration for this handler - not one Scoped copy from scanning plus a
        // second Singleton copy from AddHandler - proves the explicit registration replaced, rather than
        // duplicated, the scanned one.
        var descriptor = Assert.Single(services, s => s.ServiceType == typeof(IQueryHandler<SampleQuery, Outcome>));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddInterlocutor_WithValidateOnBuild_ThrowsAggregatingEveryProblem_WhenTheConfigurationHasAny()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var exception = Assert.Throws<InterlocutorRegistrationValidationException>(() =>
            services.AddInterlocutor(builder =>
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
    public void AddInterlocutor_WithValidateOnBuild_LeavesServicesUnmutated_WhenValidationFails()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Assert.Throws<InterlocutorRegistrationValidationException>(() =>
            services.AddInterlocutor(builder =>
                builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).ValidateOnBuild()
            )
        );

        // Assert
        Assert.Empty(services);
    }

    [Fact]
    public void AddInterlocutor_WithoutValidateOnBuild_DoesNotThrow_EvenWithAnUnhandledRequestType() =>
        AssertScanningTheTestAssemblyDoesNotThrow();

    /// <summary>
    /// Shared body for <see cref="AddInterlocutor_WithoutLoggerFactoryRegistered_DoesNotThrow"/> and
    /// <see cref="AddInterlocutor_WithoutValidateOnBuild_DoesNotThrow_EvenWithAnUnhandledRequestType"/> - both
    /// assert the same "scanning the test assembly does not throw" guarantee, from two different absence
    /// scenarios each has its own contrasting sibling test for elsewhere in this file.
    /// </summary>
    private static void AssertScanningTheTestAssemblyDoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var exception = Record.Exception(() =>
            services.AddInterlocutor(builder => builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly))
        );

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AddAuditingBehavior_RegistersAuditSinkDefault_ButAuthorizationBehaviorDoesNotOrphanIt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuthorizationBehavior()
        );

        // Assert
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IAuditSink));
        Assert.Contains(services, s => s.ServiceType == typeof(IPolicyEvaluator));
    }

    [Fact]
    public void ConsumerRegistration_BeforeBuilderCall_Wins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IAuditSink, RecordingAuditSink>();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuditingBehavior()
        );
        var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<IAuditSink>();

        // Assert
        Assert.IsType<RecordingAuditSink>(sink);
    }

    [Fact]
    public void OverrideAuditSink_AfterAddAuditingBehavior_Wins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddAuditingBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
        );
        var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<IAuditSink>();

        // Assert
        Assert.IsType<RecordingAuditSink>(sink);
    }

    [Fact]
    public async Task BehaviorExecutionOrder_MatchesBuilderCallOrder_ObservedAtRuntime()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.Second))
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new SampleQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], executionLog);
    }

    [Fact]
    public async Task SameBehaviorRegisteredTwice_ExecutesTwice()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddBehavior(typeof(RecordingBehavior<,>.First))
                .AddBehavior(typeof(RecordingBehavior<,>.First))
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new SampleQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first-before", "first-before", "first-after", "first-after"], executionLog);
    }

    [Fact]
    public void UseRecommendedChain_RegistersLoggingAuditingAndAuthorizationBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.UseRecommendedChain()
        );
        var behaviorImplementationTypes = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(s => s.ImplementationType)
            .ToList();

        // Assert
        Assert.Contains(typeof(LoggingBehavior<,>), behaviorImplementationTypes);
        Assert.Contains(typeof(AuditingBehavior<,>), behaviorImplementationTypes);
        Assert.Contains(typeof(AuthorizationBehavior<,>), behaviorImplementationTypes);
    }

    [Fact]
    public async Task UseRecommendedChain_DispatchesSuccessfully_ThroughARealMediator()
    {
        // The decisive end-to-end proof: AuthorizationBehavior and ValidationBehavior both had a structural
        // shape that made Microsoft DI throw ArgumentException on every single dispatch the instant either
        // was registered (see both types' XML remarks) - a registration-list check, like the test above,
        // could never have caught that; only an actual dispatch through a built container can. This request
        // satisfies every marker in the recommended chain (IAuditableRequest, IRequiresPolicy) and carries no
        // FluentValidation validators, so it exercises every fixed behavior on the real send path and must
        // complete without throwing.
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // UnitOfWorkBehavior<,> applies to every request once registered, per ADR-0018 - no ITransactionalRequest
        // marker required to participate - so even this pure-read query needs Lazy<ISession> resolvable. It
        // never actually begins a transaction here: nothing in this dispatch writes.
        services.AddScoped(_ => new Lazy<global::NHibernate.ISession>(RealNHibernateSession.Open));

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.UseRecommendedChain()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new AuthorizableQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task AuditingBehavior_PopulatesUserId_FromAmbientRequestContextAccessor_ThroughARealSend()
    {
        // End-to-end proof that IRequestContextAccessor flows into AuditEntry.UserId: the accessor's
        // Context is set on the same scope a real IMediator.Send dispatches through, using the library's
        // own default IRequestContextAccessor (AmbientRequestContextAccessor) rather than a hand-wired stub.
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sink = new CapturingAuditSink();
        services.AddSingleton<IAuditSink>(sink);

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var contextAccessor = scope.ServiceProvider.GetRequiredService<IRequestContextAccessor>();
        contextAccessor.Context = new RequestContext { UserId = "user-e2e" };
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new AuditOnlyQuery(), TestContext.Current.CancellationToken);

        // Assert
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("user-e2e", entry.UserId);
    }

    [Fact]
    public async Task AuditingBehavior_LeavesUserIdNull_WhenNoRequestContextIsPopulated()
    {
        // The anonymous/no-ambient-context case: the default accessor is registered but nothing ever set
        // its Context, matching a request with no authenticated caller. Auditing must still succeed.
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sink = new CapturingAuditSink();
        services.AddSingleton<IAuditSink>(sink);

        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddAuditingBehavior()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new AuditOnlyQuery(), TestContext.Current.CancellationToken);

        // Assert
        var entry = Assert.Single(sink.Entries);
        Assert.Null(entry.UserId);
    }

    [Fact]
    public async Task StageHost_WithTypeRegisteredStage_RunsBeforeAndAfterAroundTheHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddStage<RecordingStage>()
                .AddStageHost()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new SampleQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(default, result);
        Assert.Equal(["stage-before", "stage-after"], executionLog);
    }

    [Fact]
    public async Task StageHost_WithShortCircuitingDelegateStage_SkipsHandlerAndReturnsShortCircuitResponseUnchanged()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerRan = false;
        services.AddSingleton(new HandlerRanFlag(() => handlerRan = true));
        services.AddLogging();

        var declined = new Outcome(new Interlocutor.CommonKernel.ResultTypes.Failure("declined by stage"));

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddStage<ShortCircuitQuery, Outcome>(
                    (_, _) => Task.FromResult(StageResult<Outcome>.ShortCircuit(declined)),
                    null
                )
                .AddStageHost()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act: the response flows out of the mediator exactly as a handled response would --
        // nothing observing it from outside the stage host can tell it was short-circuited.
        var result = await mediator.SendAsync(new ShortCircuitQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(declined, result);
        Assert.False(handlerRan);
    }

    [Fact]
    public async Task StageHost_WithThrowingAfterHook_ExceptionIsSwallowedAndDispatchCompletes()
    {
        // Wrapper-level tests (PipelineStageBehaviorTests) cover the after-hook-exception unwind in detail -
        // this proves the same default-swallow behavior holds end-to-end through the real container and
        // mediator, not only against the behavior in isolation.
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly)
                .Pipeline.AddStage<SampleQuery, Outcome>(
                    null,
                    (_, _, _) => throw new InvalidOperationException("after-hook boom")
                )
                .AddStageHost()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert: dispatch completes normally - the after-hook's exception does not propagate out of
        // Send, matching PipelineStageBehavior's default (non-rethrowing) after-hook exception handling.
        var result = await mediator.SendAsync(new SampleQuery(), TestContext.Current.CancellationToken);
        Assert.Equal(default, result);
    }

    [Fact]
    public void AddCancellationBehavior_ResolvesWithoutThrowing_UnderMicrosoftDI()
    {
        // The exact open-generic-resolution failure mode documented on AuthorizationBehavior's remarks and
        // fixed once before (audit-followups/13): a behavior whose constrained TRequest/TResponse shape
        // throws ArgumentException from Microsoft DI's positional open-generic substitution instead of simply
        // resolving. Registering and resolving against a closed generic that satisfies the constraints is the
        // only way to prove this.
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddCancellationBehavior()
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope
            .ServiceProvider.GetServices<IPipelineBehavior<CancellableQuery, Outcome<string, Cancelled>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("CancellationBehavior"));
    }

    [Fact]
    public void AddTimeoutBehavior_ResolvesWithoutThrowing_UnderMicrosoftDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddTimeoutBehavior()
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope
            .ServiceProvider.GetServices<IPipelineBehavior<TimeBoundQuery, Outcome<string, Timeout>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("TimeoutBehavior"));
    }

    [Fact]
    public void AddRateLimitedBehavior_ResolvesWithoutThrowing_UnderMicrosoftDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(InterlocutorBuilderIntegrationTests).Assembly).Pipeline.AddRateLimitedBehavior()
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope
            .ServiceProvider.GetServices<IPipelineBehavior<ThrottledQuery, Outcome<string, RateLimited>>>()
            .ToList();

        // Assert
        Assert.Contains(behaviors, b => b.GetType().Name.StartsWith("RateLimitedBehavior"));
    }

    private sealed record CancellableQuery : IQuery<Outcome<string, Cancelled>>, ICancellable;

    // The four handler classes through AuthorizableQueryHandler below are resolved only via
    // AddInterlocutor()'s assembly-scanning DI registration - never new'd or typeof'd directly in
    // this file - so their constructors are invisible to static analysis.
#pragma warning disable S1144
    private sealed class CancellableQueryHandler : IQueryHandler<CancellableQuery, Outcome<string, Cancelled>>
    {
        public Task<Outcome<string, Cancelled>> HandleAsync(CancellableQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<string, Cancelled>>("ok");
    }

    private sealed record TimeBoundQuery : IQuery<Outcome<string, Timeout>>, IHasTimeout
    {
        public TimeSpan Budget => TimeSpan.FromSeconds(30);
    }

    private sealed class TimeBoundQueryHandler : IQueryHandler<TimeBoundQuery, Outcome<string, Timeout>>
    {
        public Task<Outcome<string, Timeout>> HandleAsync(TimeBoundQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<string, Timeout>>("ok");
    }

    private sealed record ThrottledQuery : IQuery<Outcome<string, RateLimited>>, IRateLimited
    {
        public string LimitKey => "test-limit";
    }

    private sealed class ThrottledQueryHandler : IQueryHandler<ThrottledQuery, Outcome<string, RateLimited>>
    {
        public Task<Outcome<string, RateLimited>> HandleAsync(ThrottledQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<string, RateLimited>>("ok");
    }

    private sealed record AuthorizableQuery : IQuery<Outcome<string, Forbidden>>, IAuditableRequest, IRequiresPolicy
    {
        public string ActionName => "Test.Audit";

        public string PolicyName => "Test.Policy";
    }

    private sealed class AuthorizableQueryHandler : IQueryHandler<AuthorizableQuery, Outcome<string, Forbidden>>
    {
        public Task<Outcome<string, Forbidden>> HandleAsync(AuthorizableQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<string, Forbidden>>("ok");
    }
#pragma warning restore S1144

    private sealed record SampleQuery : IQuery<Outcome>;

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(SampleQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    /// <summary>Deliberately has no handler, so ValidateOnBuild() tests have a deterministic problem to find.</summary>
    private sealed record UnhandledValidationQuery : IQuery<Outcome>;

    private sealed class RecordingStage(List<string> log) : PipelineStageBase<SampleQuery, Outcome>
    {
        public override async Task<StageResult<Outcome>> OnBeforeAsync(SampleQuery request, CancellationToken ct)
        {
            log.Add("stage-before");
            await Task.CompletedTask.ConfigureAwait(false);
            return StageResult<Outcome>.Continue;
        }

        public override async Task OnAfterAsync(SampleQuery request, Outcome result, CancellationToken ct)
        {
            log.Add("stage-after");
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private sealed record ShortCircuitQuery : IQuery<Outcome>;

    private sealed class HandlerRanFlag(Action onHandled)
    {
        public void MarkHandled() => onHandled();
    }

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class ShortCircuitQueryHandler(HandlerRanFlag flag) : IQueryHandler<ShortCircuitQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(ShortCircuitQuery query, CancellationToken ct)
        {
            flag.MarkHandled();
            return Task.FromResult(default(Outcome));
        }
    }
#pragma warning restore S1144

    private sealed class RecordingAuditSink : IAuditSink
    {
        public Task RecordAsync(AuditEntry entry, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed record AuditOnlyQuery : IQuery<Outcome>, IAuditableRequest
    {
        public string ActionName => "Test.AuditOnly";
    }

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class AuditOnlyQueryHandler : IQueryHandler<AuditOnlyQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(AuditOnlyQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }
#pragma warning restore S1144

    /// <summary>An <see cref="IAuditSink"/> that captures every recorded entry for post-dispatch assertions.</summary>
    private sealed class CapturingAuditSink : IAuditSink
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task RecordAsync(AuditEntry entry, CancellationToken ct)
        {
            this.Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    /// <summary>Container for two distinct recording behaviors so both can share the log-entry naming convention.</summary>
    private static class RecordingBehavior<TRequest, TResponse>
    {
        public sealed class First(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        {
            public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
            {
                log.Add("first-before");
                var result = await next().ConfigureAwait(false);
                log.Add("first-after");
                return result;
            }
        }

        public sealed class Second(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        {
            public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
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
