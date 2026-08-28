namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Covers <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/>'s flush-mode default, begin-on-first-write,
/// eager-begin, commit/rollback, and <see cref="IExternalStoreRequest"/> opt-out behavior per ADR-0018,
/// against a substituted <see cref="ISession"/> - the same style as this project's other behavior coverage
/// (see <see cref="SharedSessionIntegrationTests"/>'s remarks for why <see cref="ISession"/> query execution
/// itself is covered elsewhere instead).
/// </summary>
public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Handle_DispatchThatNeverWrites_NeverBeginsTransaction()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var behavior = CreateBehavior(session);

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            () => Task.FromResult<OperationOutcome<Success, Failure>>(default(Success)),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        session.DidNotReceive().BeginTransaction();
    }

    [Fact]
    public async Task Handle_SetsFlushModeToCommit_BeforeHandlerRuns()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var behavior = CreateBehavior(session);
        var flushModeWhenHandlerRan = FlushMode.Auto;

        // Act
        await behavior.Handle(
            new PlainRequest(),
            () =>
            {
                flushModeWhenHandlerRan = session.FlushMode;
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        // Prevents NHibernate's default auto-flush-before-query from writing staged changes ahead of any
        // transaction this behavior opens - see this type's remarks.
        Assert.Equal(FlushMode.Commit, flushModeWhenHandlerRan);
    }

    [Fact]
    public async Task Handle_WriteStagedDirectlyOnSession_BeginsTransactionAndCommitsOnSuccess()
    {
        // Arrange
        var (session, transaction) = CreateStubbedSession();
        var behavior = CreateBehavior(session);

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            () =>
            {
                // IsDirty() reflects the session's own tracked state regardless of which access path staged
                // it - a real ISession would report true after a direct Save() the same way.
                session.IsDirty().Returns(true);
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ClassifiedFailure_RollsBack_NotCommit()
    {
        // Arrange
        var (session, transaction) = CreateStubbedSession();
        var behavior = CreateBehavior(session);

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            () =>
            {
                session.IsDirty().Returns(true);
                return Task.FromResult<OperationOutcome<Success, Failure>>(new Failure("rejected"));
            },
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        await transaction.Received(1).RollbackAsync(CancellationToken.None);
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TransactionalRequest_BeginsEagerly_BeforeHandlerWrites()
    {
        // Arrange
        var (session, _) = CreateStubbedSession();
        var behavior = new UnitOfWorkBehavior<TransactionalRequest, OperationOutcome<Success, Failure>>(
            new Lazy<ISession>(() => session)
        );
        var beginCallsBeforeHandler = 0;

        // Act
        await behavior.Handle(
            new TransactionalRequest(),
            () =>
            {
                beginCallsBeforeHandler = session
                    .ReceivedCalls()
                    .Count(c => c.GetMethodInfo().Name == nameof(ISession.BeginTransaction));
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.Equal(1, beginCallsBeforeHandler);
    }

    [Fact]
    public async Task Handle_ExternalStoreRequest_SkipsSession_EvenThoughAlsoTransactional()
    {
        // Arrange
        var session = new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved"));
        var behavior = new UnitOfWorkBehavior<RequestClaimingBothMarkers, OperationOutcome<string, Failure>>(session);
        var handled = false;

        // Act
        var result = await behavior.Handle(
            new RequestClaimingBothMarkers(),
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
    public async Task Handle_UnitOfWorkCancel_RollsBackImmediately_AndPreservesHandlerOutcome()
    {
        // Arrange
        var (session, transaction) = CreateStubbedSession();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        var behavior = CreateBehavior(session);

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            async () =>
            {
                session.IsDirty().Returns(true);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);

                // The handler cancels, then still returns its own Success outcome - Cancel is not a
                // short-circuit primitive, so the handler retains control of what it returns.
                await unitOfWork.Cancel(CancellationToken.None);

                // Cancel's Session.Clear() resets IsDirty() to false, same as a real session with nothing
                // pending after a rollback.
                session.IsDirty().Returns(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        await transaction.Received(1).RollbackAsync(CancellationToken.None);
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        session.Received(1).Clear();
    }

    [Fact]
    public async Task Handle_UsesInjectedClassifier_NotOutcomeShape_ToDecideCommitVersusRollback()
    {
        // Arrange
        var (session, transaction) = CreateStubbedSession();
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(true);
        var behavior = new UnitOfWorkBehavior<PlainRequest, OperationOutcome<Success, Failure>>(
            new Lazy<ISession>(() => session),
            classifier
        );

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            () =>
            {
                session.IsDirty().Returns(true);
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        await transaction.Received(1).RollbackAsync(CancellationToken.None);
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Builds a substituted <see cref="ISession"/>/<see cref="ITransaction"/> pair whose
    /// <see cref="ISession.Transaction"/> and <see cref="ITransaction.IsActive"/> track
    /// <see cref="ISession.BeginTransaction()"/>/<see cref="ITransaction.CommitAsync"/>/<see cref="ITransaction.RollbackAsync"/>
    /// as real side effects - matching real NHibernate, where beginning a transaction sets
    /// <see cref="ISession.Transaction"/> and completing one (either way) ends its <see cref="ITransaction.IsActive"/>
    /// life, which bare NSubstitute stubbing otherwise leaves disconnected from the method calls.
    /// </summary>
    private static (ISession Session, ITransaction Transaction) CreateStubbedSession()
    {
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();

        var transactionActive = false;
        session
            .BeginTransaction()
            .Returns(_ =>
            {
                transactionActive = true;
                return transaction;
            });
        session.Transaction.Returns(_ => transactionActive ? transaction : null);
        transaction.IsActive.Returns(_ => transactionActive);
        transaction.When(t => t.CommitAsync(Arg.Any<CancellationToken>())).Do(_ => transactionActive = false);
        transaction.When(t => t.RollbackAsync(Arg.Any<CancellationToken>())).Do(_ => transactionActive = false);

        return (session, transaction);
    }

    private static UnitOfWorkBehavior<PlainRequest, OperationOutcome<Success, Failure>> CreateBehavior(ISession session) =>
        new(new Lazy<ISession>(() => session));

    private sealed record PlainRequest : ICommand<OperationOutcome<Success, Failure>>;

    private sealed record TransactionalRequest : ICommand<OperationOutcome<Success, Failure>>, ITransactionalRequest;

    private sealed record RequestClaimingBothMarkers
        : ICommand<OperationOutcome<string, Failure>>,
            ITransactionalRequest,
            IExternalStoreRequest;
}
