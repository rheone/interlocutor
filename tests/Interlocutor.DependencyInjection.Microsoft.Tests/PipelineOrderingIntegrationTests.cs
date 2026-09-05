namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::FluentValidation;
using global::NHibernate;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.Logging;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves the canonical pipeline order documented on <see cref="DependencyInjection.Abstractions.InterlocutorPipelineBuilder.UseRecommendedChain"/>
/// (Logging → Auditing → Authorization → Validation → Transaction) by registering the five real behaviors
/// together through that exact method and sending an actual request through the composed pipeline - not by
/// inspecting the registration list, which only proves presence, not order.
/// </summary>
/// <remarks>
/// No test double stands in for any of the five behaviors: each one's only externally observable side effect
/// (its logger call, audit sink write, policy evaluation, validator invocation, or transaction begin/commit)
/// is what gets recorded. <see cref="IAuditSink"/> and <see cref="IPolicyEvaluator"/> are overridden inline
/// via the builder itself; <see cref="IValidator{T}"/> is a small hand-written recording fake; and
/// <see cref="ISession"/> is a real, mapping-free in-memory NHibernate session (see
/// <see cref="ConfigureSharedDependencies"/>) rather than a mocked stand-in, so the real onion nesting -
/// including a real transaction begin/commit - is what produces the observed order.
/// </remarks>
public sealed class PipelineOrderingIntegrationTests
{
    [Fact]
    public async Task UseRecommendedChain_ObservedExecutionOrder_MatchesLoggingAuditingAuthorizationValidationTransaction()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        using var session = ConfigureSharedDependencies(services, log);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOrderingIntegrationTests).Assembly)
                .Pipeline.UseRecommendedChain()
                .OverrideAuditSink<RecordingAuditSink>()
                .OverridePolicyEvaluator<RecordingPolicyEvaluator>()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var sendResult = await mediator.SendAsync(new OrderedRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(sendResult.TryGetValue(out Success _));
        // The real transaction never stays open past a successful dispatch - it was either committed or
        // rolled back; the ordered log below (ending in "transaction-commit") proves which.
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // Inbound (before the handler runs): only Authorization, Validation, and Transaction have an
        // observable "entering" side effect - Logging and Auditing only act after next() returns. Outbound
        // (after the handler runs, in reverse depth order): only Transaction, Auditing, and Logging have an
        // observable "leaving" side effect - Authorization and Validation return next()'s result unchanged.
        // The full sequence below is therefore the strongest order proof this black-box send can produce:
        // Authorization before Validation before Transaction proves the inbound half, and Transaction before
        // Auditing before Logging proves the outbound half, of the documented canonical order.
        Assert.Equal(
            ["authorization", "validation", "transaction-begin", "handler", "transaction-commit", "auditing", "logging"],
            log
        );
    }

    [Fact]
    public async Task AdjacentStagesSwapped_ObservedExecutionOrder_NoLongerMatchesCanonicalOrder()
    {
        // Proves the assertion above is a real ordered-sequence check and not merely "all five ran": swapping
        // Authorization and Validation's builder-call order - both of which have a directly observable
        // "entering" side effect - inverts their relative position in the observed log, which the canonical
        // test's exact-sequence assertion would catch immediately.
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        using var session = ConfigureSharedDependencies(services, log);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOrderingIntegrationTests).Assembly)
                .Pipeline.AddLoggingBehavior()
                .AddAuditingBehavior()
                // Swapped: Validation before Authorization, the reverse of UseRecommendedChain's order.
                .AddValidationBehavior()
                .AddAuthorizationBehavior()
                .AddUnitOfWorkBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
                .OverridePolicyEvaluator<RecordingPolicyEvaluator>()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new OrderedRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(
            ["authorization", "validation", "transaction-begin", "handler", "transaction-commit", "auditing", "logging"],
            log
        );
        // Validation now enters before Authorization, matching the swapped builder-call order.
        Assert.Equal(
            ["validation", "authorization", "transaction-begin", "handler", "transaction-commit", "auditing", "logging"],
            log
        );
    }

    /// <summary>
    /// Registers the recording <see cref="ILogger{TCategoryName}"/> provider, <see cref="IValidator{T}"/>,
    /// and a real NHibernate <see cref="ISession"/> shared by both tests - everything the five real
    /// behaviors resolve besides the audit sink and policy evaluator, which are overridden inline via the
    /// builder itself.
    /// </summary>
    /// <remarks>
    /// The session is real (see <see cref="RealNHibernateSession"/>) rather than an
    /// <c>ISession</c>/<c>ITransaction</c> pair hand-simulating <c>BeginTransaction</c>/<c>Commit</c> side
    /// effects: <see cref="global::NHibernate.SessionExtensions.GetCurrentTransaction(ISession)"/> - which
    /// <c>UnitOfWorkBehavior&lt;,&gt;</c> now reads instead of the obsolete <c>ISession.Transaction</c> -
    /// resolves through real session internals a mock can't stand in for. "transaction-begin" is recorded
    /// by <see cref="OrderedRequestHandler"/> observing a real active transaction when it runs (proving the
    /// eager begin already happened); "transaction-commit"/"transaction-rollback" are recorded by
    /// <see cref="RecordingLogger"/> from the debug-level line <c>UnitOfWorkBehavior&lt;,&gt;</c> already
    /// logs at that exact moment - both real side effects, not simulated ones.
    /// </remarks>
    private static ISession ConfigureSharedDependencies(ServiceCollection services, List<string> log)
    {
        services.AddSingleton(log);
        services.AddLogging(loggingBuilder =>
        {
            // UnitOfWorkBehavior<,>'s commit/rollback line - which RecordingLogger turns into this test's
            // "transaction-commit"/"transaction-rollback" log entries - is logged at Debug level.
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddProvider(new RecordingLoggerProvider(log));
        });
        services.AddSingleton<IValidator<OrderedRequest>>(new RecordingValidator(log));

        var session = RealNHibernateSession.Open();
        services.AddScoped(_ => session);
        // NHibernate.UnitOfWorkBehavior<,> takes a Lazy<ISession> factory, not ISession directly - Microsoft
        // DI has no built-in Lazy<T> support, so the factory is registered explicitly.
        services.AddScoped(sp => new Lazy<ISession>(() => sp.GetRequiredService<ISession>()));
        return session;
    }

    private sealed record OrderedRequest
        : ICommand<Outcome<Success, Forbidden>>,
            ITransactionalRequest,
            IAuditableRequest,
            IRequiresPolicy
    {
        public string ActionName => "Test.Ordered";

        public string PolicyName => "Test.Policy";
    }

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class OrderedRequestHandler(Lazy<ISession> session, List<string> log)
        : ICommandHandler<OrderedRequest, Outcome<Success, Forbidden>>
    {
        public Task<Outcome<Success, Forbidden>> HandleAsync(OrderedRequest command, CancellationToken ct)
        {
            // UnitOfWorkBehavior<,> begins a transaction eagerly for ITransactionalRequest, before next()
            // runs - checking the real session here proves that already happened, rather than assuming it
            // from timing alone.
            if (session.Value.GetCurrentTransaction() is { IsActive: true })
            {
                log.Add("transaction-begin");
            }

            log.Add("handler");
            // The success payload must be the kernel's own Success type, not an arbitrary value: the default
            // outcome classifier - which every real behavior in this chain shares - fail-closes on unknown
            // success types, and UnitOfWorkBehavior would (correctly) roll back instead of commit for one.
            return Task.FromResult<Outcome<Success, Forbidden>>(default(Success));
        }
    }
#pragma warning restore S1144

    /// <summary>A validator whose single always-passing rule records that it ran, proving Validation's inbound side effect.</summary>
    // Deliberately shaped against OrderedRequest's Outcome<Success, Forbidden> response (no validation case) -
    // this validator's rule never fails, so ValidationBehavior never needs to construct one; it exists only to
    // prove ordering, not real validation-shape correctness.
#pragma warning disable INTERLOC010
    private sealed class RecordingValidator : AbstractValidator<OrderedRequest>
    {
        public RecordingValidator(List<string> log) =>
            this.RuleFor(request => request)
                .Must(_ =>
                {
                    log.Add("validation");
                    return true;
                });
    }
#pragma warning restore INTERLOC010

    /// <summary>An <see cref="IPolicyEvaluator"/> that always authorizes, recording that it ran - Authorization's inbound side effect.</summary>
    private sealed class RecordingPolicyEvaluator(List<string> log) : IPolicyEvaluator
    {
        public Task<bool> EvaluateAsync(string policyName, object request, CancellationToken ct)
        {
            log.Add("authorization");
            return Task.FromResult(true);
        }
    }

    /// <summary>An <see cref="IAuditSink"/> recording that it ran - Auditing's outbound side effect.</summary>
    private sealed class RecordingAuditSink(List<string> log) : IAuditSink
    {
        public Task RecordAsync(AuditEntry entry, CancellationToken ct)
        {
            log.Add("auditing");
            return Task.CompletedTask;
        }
    }

    /// <summary>An <see cref="ILoggerProvider"/> whose loggers record every informational message - Logging's outbound side effect.</summary>
    private sealed class RecordingLoggerProvider(List<string> log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(log, categoryName);

        public void Dispose() { }
    }

    /// <summary>
    /// Records every informational message, and translates <c>UnitOfWorkBehavior&lt;,&gt;</c>'s own
    /// debug-level commit/rollback line into this test's "transaction-commit"/"transaction-rollback" log
    /// entries - both skipped for the <c>"Interlocutor"</c> category, which <c>AddInterlocutor()</c>'s own
    /// opt-in registration-report log
    /// (<see cref="Interlocutor.DependencyInjection.Microsoft.InterlocutorServiceCollectionExtensions"/>) uses,
    /// fires once at configuration time, and is not part of the pipeline-order sequence this test proves.
    /// </summary>
    private sealed class RecordingLogger(List<string> log, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (categoryName == "Interlocutor")
            {
                return;
            }

            if (logLevel == LogLevel.Information)
            {
                log.Add("logging");
            }
            else if (logLevel == LogLevel.Debug)
            {
                var message = formatter(state, exception);
                if (message.StartsWith("Committed transaction", StringComparison.Ordinal))
                {
                    log.Add("transaction-commit");
                }
                else if (message.StartsWith("Rolled back transaction", StringComparison.Ordinal))
                {
                    log.Add("transaction-rollback");
                }
            }
        }
    }
}
