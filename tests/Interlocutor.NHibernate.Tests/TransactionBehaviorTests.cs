namespace Interlocutor.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.NHibernate;
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
        var session = new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved"));
        var behavior = new NHibernate.TransactionBehavior<
            RequestClaimingBothTransactionMarkers,
            OperationOutcome<string, Failure>
        >(session);
        var handled = false;

        var result = await behavior.Handle(
            new RequestClaimingBothTransactionMarkers(1),
            () =>
            {
                handled = true;
                return Task.FromResult<OperationOutcome<string, Failure>>("done");
            },
            CancellationToken.None
        );

        Assert.True(handled);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public void TRequest_Constraint_RejectsNonTransactionalRequestAtCompileTime()
    {
        // A request that implements neither marker cannot close TransactionBehavior<,>'s TRequest parameter at
        // all - proven here via reflection (the same MakeGenericType mechanism both Microsoft DI and Autofac
        // use internally to decide whether an open generic registration applies).
        var openGeneric = typeof(TransactionBehavior<,>);
        var nonTransactionalRequest = typeof(NonTransactionalRequest);
        var response = typeof(OperationOutcome<string, Failure>);

        var ex = Assert.Throws<ArgumentException>(() => openGeneric.MakeGenericType(nonTransactionalRequest, response));
        Assert.Contains("constraint", ex.Message, StringComparison.OrdinalIgnoreCase);

        // The same closure succeeds once the request satisfies ITransactionalRequest.
        var closed = openGeneric.MakeGenericType(typeof(TransactionalTestRequest), response);
        Assert.NotNull(closed);
    }

    [Fact]
    public async Task Handle_CommitsSession_WhenClassifierReportsSuccess()
    {
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: false));

        var result = await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>("done"),
            CancellationToken.None
        );

        Assert.Equal("done", result.Value);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).CommitAsync(CancellationToken.None);
        await transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RollsBackSession_WhenClassifierReportsFailure()
    {
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: true));
        var failure = new Failure("something went wrong");

        var result = await behavior.Handle(
            new TransactionalTestRequest(1),
            () => Task.FromResult<OperationOutcome<string, Failure>>(failure),
            CancellationToken.None
        );

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
        var persistenceFailure = new InvalidOperationException("flush failed");
        var (behavior, session, transaction) = CreateBehavior(classifier: StubClassifier(isFailure: false));
        transaction.CommitAsync(Arg.Any<CancellationToken>()).ThrowsAsync(persistenceFailure);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TransactionalTestRequest(1),
                () => Task.FromResult<OperationOutcome<string, Failure>>("done"),
                CancellationToken.None
            )
        );

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

        await behavior.Handle(new TransactionalTestRequest(1), () => Task.FromResult(outcome), CancellationToken.None);

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
}
