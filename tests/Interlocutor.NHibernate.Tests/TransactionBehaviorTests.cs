namespace Interlocutor.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.NHibernate;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class TransactionBehaviorTests
{
    /// <summary>
    /// Gets classified outcomes and their expected failure disposition, used by
    /// <see cref="Handle_CommitOrRollbackDecision_MatchesEntityFrameworkCoreBehavior_ForIdenticalClassifiedInput"/>.
    /// </summary>
    /// <value>Theory rows of an outcome factory paired with the expected <c>IsFailure</c> classification.</value>
    public static TheoryData<
        Func<OperationOutcome<Success, NotFound, Forbidden, Conflict, ValidationErrors, Failure>>,
        bool
    > ClassifiedOutcomes =>
        new()
        {
            { () => default(Success), false },
            { () => new NotFound("missing"), true },
            { () => new Forbidden("denied"), true },
            { () => new Conflict("duplicate", "E_DUP"), true },
            { () => new ValidationErrors(["required"]), true },
            { () => new Failure("boom"), true },
        };

    [Fact]
    public async Task ExternalStoreRequest_SkipsSession_EvenThoughAlsoTransactional()
    {
        // Arrange
        var session = new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved"));
        var behavior = new NHibernate.TransactionBehavior<
            RequestClaimingBothTransactionMarkers,
            OperationOutcome<string, Failure>
        >(session);
        var handled = false;

        // Act
        var result = await behavior.Handle(
            new RequestClaimingBothTransactionMarkers(1),
            () =>
            {
                handled = true;
                return Task.FromResult<OperationOutcome<string, Failure>>("done");
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(handled);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public void TRequest_Constraint_RejectsNonTransactionalRequestAtCompileTime()
    {
        // Arrange
        // A request that implements neither marker cannot close TransactionBehavior<,>'s TRequest parameter at
        // all - proven here via reflection (the same MakeGenericType mechanism both Microsoft DI and Autofac
        // use internally to decide whether an open generic registration applies).
        var openGeneric = typeof(TransactionBehavior<,>);
        var nonTransactionalRequest = typeof(NonTransactionalRequest);
        var response = typeof(OperationOutcome<string, Failure>);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => openGeneric.MakeGenericType(nonTransactionalRequest, response));
        Assert.Contains("constraint", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Act & Assert
        // The same closure succeeds once the request satisfies ITransactionalRequest.
        var closed = openGeneric.MakeGenericType(typeof(TransactionalTestRequest), response);
        Assert.NotNull(closed);
    }

    [Fact]
    public async Task Handle_CommitsSession_WhenClassifierReportsSuccess()
    {
        // Arrange
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: false));

        // Act
        var result = await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>("done"),
            CancellationToken.None
        );

        // Assert
        Assert.Equal("done", result.Value);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).CommitAsync(CancellationToken.None);
        await transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RollsBackSession_WhenClassifierReportsFailure()
    {
        // Arrange
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: true));
        var failure = new Failure("something went wrong");

        // Act
        var result = await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>(failure),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(failure, result.Value);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).RollbackAsync(CancellationToken.None);
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RollsBackAndRethrowsOriginalException_WhenCommitFailsDuringPersistence()
    {
        // Unlike Interlocutor.EntityFrameworkCore.TransactionBehavior<,,> - which wraps its explicit
        // SaveChangesAsync call in a try/catch that calls RollbackAsync before rethrowing - NHibernate's
        // CommitAsync is what performs the flush (persistence), and TransactionBehavior<,> has no try/catch
        // around it. Rollback of an uncommitted transaction instead happens implicitly: the `using var
        // transaction = ...` statement always disposes the transaction on the way out of Handle, and an
        // NHibernate ITransaction that is disposed without having committed rolls itself back - the same
        // using-scoped rollback contract as a plain ADO.NET transaction. This test locks in that behavior:
        // the persistence exception propagates unmodified, and the transaction is disposed (not explicitly
        // rolled back) on the way out.

        // Arrange
        var persistenceFailure = new InvalidOperationException("flush failed");
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: false));
        transaction.CommitAsync(Arg.Any<CancellationToken>()).ThrowsAsync(persistenceFailure);

        // Act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TransactionalTestRequest(1),
                () => Task.FromResult<OperationOutcome<string, Failure>>("done"),
                CancellationToken.None
            )
        );

        // Assert
        Assert.Same(persistenceFailure, thrown);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).CommitAsync(CancellationToken.None);
        await transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        transaction.Received(1).Dispose();
    }

    [Theory]
    [MemberData(nameof(ClassifiedOutcomes))]
    public async Task Handle_CommitOrRollbackDecision_MatchesEntityFrameworkCoreBehavior_ForIdenticalClassifiedInput(
        Func<OperationOutcome<Success, NotFound, Forbidden, Conflict, ValidationErrors, Failure>> makeOutcome,
        bool expectedIsFailure
    )
    {
        // Arrange
        var outcome = makeOutcome();

        // Sanity check: confirm the shared default classifier - the exact OutcomeClassifierBuilder.DefaultClassifier
        // instance both this behavior and Interlocutor.EntityFrameworkCore.TransactionBehavior<,,> fall back to
        // when constructed without an explicit classifier - classifies this outcome the way the test expects.
        Assert.Equal(expectedIsFailure, OutcomeClassifierBuilder.DefaultClassifier.IsFailure(outcome));

        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        session.BeginTransaction().Returns(transaction);
        var behavior = new TransactionBehavior<
            TransactionalTestRequest,
            OperationOutcome<Success, NotFound, Forbidden, Conflict, ValidationErrors, Failure>
        >(new Lazy<ISession>(() => session));

        // Act
        await behavior.Handle(new TransactionalTestRequest(1), () => Task.FromResult(outcome), CancellationToken.None);

        // Assert
        // src/Interlocutor.NHibernate/TransactionBehavior.cs and
        // src/Interlocutor.EntityFrameworkCore/TransactionBehavior.cs both gate commit vs. rollback with the
        // identical `!this.classifier.IsFailure(result)` expression, evaluated against the same shared classifier
        // type. Having proven above that the classification of this outcome matches expectations, both behaviors
        // are guaranteed - by sharing that expression and classifier - to reach the same commit/rollback
        // disposition for it. This assertion is the NHibernate side of that agreement.
        if (expectedIsFailure)
        {
            await transaction.Received(1).RollbackAsync(CancellationToken.None);
            await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        }
        else
        {
            await transaction.Received(1).CommitAsync(CancellationToken.None);
            await transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Handle_LogsDebugCommitLine_WhenClassifierReportsSuccess()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        session.BeginTransaction().Returns(transaction);
        var logger = new CapturingLogger<TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>>>();
        // "done" is a bare string, not one of the kernel's registered Success/Failure case types, so the
        // *default* classifier would fail-close it as a failure (see OutcomeClassifierBuilder's fail-closed
        // policy for unregistered types) - an explicit success-reporting classifier is required here, exactly
        // like Handle_CommitsSession_WhenClassifierReportsSuccess above.
        var behavior = new TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>>(
            new Lazy<ISession>(() => session),
            StubClassifier(isFailure: false),
            logger: logger
        );

        // Act
        await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>("done"),
            CancellationToken.None
        );

        // Assert
        // Confirms the commit/rollback diagnostic (ticket 06 follow-up) fires at Debug, distinct from
        // LoggingBehavior's Information-level line - see the XML remarks on TransactionBehavior for why
        // the two aren't duplicates. Checking the message text (not just the level) proves the wording is
        // commit-specific, not just a Debug line that happened to fire.
        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains("Committed", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Rolled back", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_LogsDebugRollbackLine_WhenClassifierReportsFailure()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        session.BeginTransaction().Returns(transaction);
        var logger = new CapturingLogger<TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>>>();
        // Failure is one of the kernel's registered case types, so the *default* classifier - unlike the
        // "done" bare string in the commit-line test above - already reports it as a failure with label
        // "Failure" without needing a stub classifier; using the real default classifier here also lets the
        // describer produce real label text for the assertion below.
        var behavior = new TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>>(
            new Lazy<ISession>(() => session),
            logger: logger
        );

        // Act
        await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>(new Failure("boom")),
            CancellationToken.None
        );

        // Assert
        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains("Rolled back", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Committed", entry.Message, StringComparison.Ordinal);
        Assert.Contains("Failure", entry.Message, StringComparison.Ordinal);
    }

    private static IOutcomeClassifier StubClassifier(bool isFailure)
    {
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(isFailure);
        return classifier;
    }

    private static (
        TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>> Behavior,
        ISession Session,
        ITransaction Transaction
    ) CreateBehavior(IOutcomeClassifier classifier)
    {
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        session.BeginTransaction().Returns(transaction);
        var behavior = new TransactionBehavior<TransactionalTestRequest, OperationOutcome<string, Failure>>(
            new Lazy<ISession>(() => session),
            classifier
        );

        return (behavior, session, transaction);
    }

    private sealed record NonTransactionalRequest(int Value) : ICommand<OperationOutcome<string, Failure>>;

    private sealed record TransactionalTestRequest(int Value)
        : ICommand<OperationOutcome<string, Failure>>,
            ITransactionalRequest;

    /// <summary>
    /// A request implementing both <see cref="ITransactionalRequest"/> and <see cref="IExternalStoreRequest"/> -
    /// the malformed-but-not-impossible case the compile-time <c>TRequest : ITransactionalRequest</c> constraint
    /// cannot rule out (a constraint can only require a marker's presence, never another marker's absence), so
    /// <see cref="TransactionBehavior{TRequest,TResponse}"/> retains a residual runtime check for it.
    /// </summary>
    private sealed record RequestClaimingBothTransactionMarkers(int Value)
        : ICommand<OperationOutcome<string, Failure>>,
            ITransactionalRequest,
            IExternalStoreRequest;

    /// <summary>
    /// Minimal <see cref="ILogger{TCategoryName}"/> fake that records every <see cref="Log"/> call - used
    /// instead of <c>Substitute.For&lt;ILogger&lt;...&gt;&gt;()</c> because that requires proxying a closed
    /// generic over the private nested <see cref="TransactionalTestRequest"/> type, which Castle DynamicProxy
    /// refuses without an <c>InternalsVisibleTo</c> grant.
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => this.Entries.Add((logLevel, formatter(state, exception)));
    }
}
