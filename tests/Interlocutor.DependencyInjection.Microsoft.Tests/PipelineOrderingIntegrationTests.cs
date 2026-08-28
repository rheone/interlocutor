namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::FluentValidation;
using global::NHibernate;
using global::NSubstitute;
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
/// is what gets recorded. Only the dependencies each behavior resolves - <see cref="ILogger{TCategoryName}"/>,
/// <see cref="IAuditSink"/>, <see cref="IPolicyEvaluator"/>, <see cref="IValidator{T}"/>, and
/// <see cref="ISession"/> - are recording stand-ins, so the real onion nesting is what produces the observed order.
/// </remarks>
public sealed class PipelineOrderingIntegrationTests
{
    [Fact]
    public async Task UseRecommendedChain_ObservedExecutionOrder_MatchesLoggingAuditingAuthorizationValidationTransaction()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        var transaction = ConfigureSharedDependencies(services, log);

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
        var sendResult = await mediator.Send(new OrderedRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(sendResult.IsSuccess);
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());

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
        ConfigureSharedDependencies(services, log);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(PipelineOrderingIntegrationTests).Assembly)
                .Pipeline.AddLoggingBehavior()
                .AddAuditingBehavior()
                // Swapped: Validation before Authorization, the reverse of UseRecommendedChain's order.
                .AddValidationBehavior()
                .AddAuthorizationBehavior()
                .AddTransactionBehavior()
                .OverrideAuditSink<RecordingAuditSink>()
                .OverridePolicyEvaluator<RecordingPolicyEvaluator>()
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new OrderedRequest(), TestContext.Current.CancellationToken);

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
    /// and NHibernate <see cref="ISession"/>/<see cref="ITransaction"/> stand-ins shared by both tests -
    /// everything the five real behaviors resolve besides the audit sink and policy evaluator, which are
    /// overridden inline via the builder itself.
    /// </summary>
    private static ITransaction ConfigureSharedDependencies(ServiceCollection services, List<string> log)
    {
        services.AddSingleton(log);
        services.AddLogging(loggingBuilder => loggingBuilder.AddProvider(new RecordingLoggerProvider(log)));
        services.AddSingleton<IValidator<OrderedRequest>>(new RecordingValidator(log));

        // Configure return values before attaching the recording side effects below: the call expression
        // inside Returns(...) is itself a real invocation NSubstitute uses to identify which member is being
        // configured, and if a When/Do side effect were already attached it would fire during this setup
        // call, polluting the log before the request is even sent.
        var transaction = Substitute.For<ITransaction>();
        transaction.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var session = Substitute.For<ISession>();
        session.BeginTransaction().Returns(transaction);

        transaction.When(t => t.CommitAsync(Arg.Any<CancellationToken>())).Do(_ => log.Add("transaction-commit"));
        session.When(s => s.BeginTransaction()).Do(_ => log.Add("transaction-begin"));

        services.AddScoped(_ => session);
        // NHibernate.TransactionBehavior<,> takes a Lazy<ISession> factory, not ISession directly - Microsoft
        // DI has no built-in Lazy<T> support, so the factory is registered explicitly.
        services.AddScoped(sp => new Lazy<ISession>(() => sp.GetRequiredService<ISession>()));
        return transaction;
    }

    private sealed record OrderedRequest
        : ICommand<OperationOutcome<Success, Forbidden>>,
            ITransactionalRequest,
            IAuditableRequest,
            IRequiresPolicy
    {
        public string ActionName => "Test.Ordered";

        public string PolicyName => "Test.Policy";
    }

    private sealed class OrderedRequestHandler(List<string> log)
        : ICommandHandler<OrderedRequest, OperationOutcome<Success, Forbidden>>
    {
        public Task<OperationOutcome<Success, Forbidden>> Handle(OrderedRequest command, CancellationToken ct)
        {
            log.Add("handler");
            // The success payload must be the kernel's own Success type, not an arbitrary value: the default
            // outcome classifier - which every real behavior in this chain shares - fail-closes on unknown
            // success types, and TransactionBehavior would (correctly) roll back instead of commit for one.
            return Task.FromResult<OperationOutcome<Success, Forbidden>>(default(Success));
        }
    }

    /// <summary>A validator whose single always-passing rule records that it ran, proving Validation's inbound side effect.</summary>
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
    /// Records every informational message under any category except <c>"Interlocutor"</c> - the category
    /// <c>AddInterlocutor()</c>'s own opt-in registration-report log
    /// (<see cref="Interlocutor.DependencyInjection.Microsoft.InterlocutorServiceCollectionExtensions"/>) uses,
    /// which fires once at configuration time and is not part of the pipeline-order sequence this test proves.
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
            if (logLevel == LogLevel.Information && categoryName != "Interlocutor")
            {
                log.Add("logging");
            }
        }
    }
}
