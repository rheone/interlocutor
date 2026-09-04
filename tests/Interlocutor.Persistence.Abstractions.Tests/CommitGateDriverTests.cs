namespace Interlocutor.Persistence.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Covers <see cref="CommitGateDriver"/>'s shared begin/run-next/check-pending-work/commit-or-rollback
/// algorithm directly, against a mocked <see cref="ICommitGateSession"/> and a mocked <c>next()</c> delegate,
/// per <c>documentation/testing.md</c>'s "mock the <c>next()</c> delegate" guidance - so the algorithm itself
/// is verified once at the seam that owns it, rather than only indirectly through each store's own
/// <c>UnitOfWorkBehaviorTests</c>.
/// </summary>
public sealed class CommitGateDriverTests
{
    [Fact]
    public async Task Handle_ExternalStoreRequest_NeverInvokesSessionFactory()
    {
        // Arrange
        var sessionFactoryInvoked = false;
        var classifier = Substitute.For<IOutcomeClassifier>();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.Handle(
            new ExternalStoreRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () =>
            {
                sessionFactoryInvoked = true;
                return Substitute.For<ICommitGateSession>();
            },
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);

        // The bypass guarantee: a request that opts out of local transaction management never pays for
        // resolving the underlying store session at all - see CommitGateDriver's own remarks.
        Assert.False(sessionFactoryInvoked);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_BeginsTransactionEagerly_BeforeNextRuns()
    {
        // Arrange
        var session = CreateSession();
        var classifier = ClassifierReturning(isFailure: false);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();
        var transactionActiveBeforeNext = false;

        // Act
        await CommitGateDriver.Handle(
            new TransactionalRequest(),
            () =>
            {
                transactionActiveBeforeNext = session.HasActiveTransaction;
                return Task.FromResult<Outcome<Success, Failure>>(default(Success));
            },
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(transactionActiveBeforeNext);
        await session.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PendingWorkAfterNext_CommitsAndDoesNotRollBack()
    {
        // Arrange - no eager begin, but the session reports staged work once next() completes, matching a
        // write staged directly on the session with no transaction opened yet.
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var classifier = ClassifierReturning(isFailure: false);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.Handle(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        await session.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EagerTransactionWithNoPendingWork_ClassifiedAsFailure_RollsBackAndDoesNotCommit()
    {
        // Arrange - the eager-begin transaction opened for a check-then-act read still has to be closed even
        // though nothing ended up pending, and a classified failure means that closure is a rollback, not a
        // commit; distinct from "nothing to finalize", which only applies when no transaction was ever opened.
        var session = CreateSession();
        var classifier = ClassifierReturning(isFailure: true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.Handle(
            new TransactionalRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(new Failure("rejected")),
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        await session.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

        // Only the one eager begin - the post-next() check must not begin a second transaction on top of it.
        await session.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoActiveTransactionAndNoPendingWork_NeitherCommitsNorRollsBack()
    {
        // Arrange - a pure read (or a dispatch that short-circuited before writing): nothing was ever staged,
        // so there is nothing to finalize at all.
        var session = CreateSession();
        var classifier = Substitute.For<IOutcomeClassifier>();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.Handle(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        await session.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        classifier.DidNotReceive().IsFailure(Arg.Any<object?>());
    }

    [Fact]
    public async Task Handle_NextThrows_PropagatesException_WithoutCommittingOrRollingBack()
    {
        // Arrange - the driver has no try/catch of its own around next(): an exception is a bug/infra fault,
        // not an outcome to classify, so it propagates unchanged and neither finalization path runs here.
        var session = CreateSession();
        var classifier = Substitute.For<IOutcomeClassifier>();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();
        var handlerException = new InvalidOperationException("handler blew up");

        // Act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CommitGateDriver.Handle(
                new PlainRequest(),
                Task<Outcome<Success, Failure>> () => throw handlerException,
                () => session,
                classifier,
                describer,
                logger,
                CancellationToken.None
            )
        );

        // Assert
        Assert.Same(handlerException, thrown);
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitPath_LogsDebugLine_WithRequestTypeAndDescribedOutcome()
    {
        // Arrange
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var classifier = ClassifierReturning(isFailure: false);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        describer.Describe(Arg.Any<object?>()).Returns("Success");
        var logger = Substitute.For<ILogger>();

        // Act
        await CommitGateDriver.Handle(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert - one debug-level line naming the commit decision, per CommitGateDriver's diagnostics contract.
        logger
            .Received(1)
            .Log(
                Arg.Is(LogLevel.Debug),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        describer.Received(1).Describe(Arg.Any<object?>());
    }

    [Fact]
    public async Task Handle_RollbackPath_LogsDebugLine_WithRequestTypeAndDescribedOutcome()
    {
        // Arrange
        var session = CreateSession();
        var classifier = ClassifierReturning(isFailure: true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        describer.Describe(Arg.Any<object?>()).Returns("Failure");
        var logger = Substitute.For<ILogger>();

        // Act
        await CommitGateDriver.Handle(
            new TransactionalRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(new Failure("rejected")),
            () => session,
            classifier,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert - one debug-level line naming the rollback decision, per CommitGateDriver's diagnostics contract.
        logger
            .Received(1)
            .Log(
                Arg.Is(LogLevel.Debug),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        describer.Received(1).Describe(Arg.Any<object?>());
    }

    /// <summary>
    /// Builds a fake <see cref="ICommitGateSession"/> whose <see cref="ICommitGateSession.HasActiveTransaction"/>
    /// flips to <see langword="true"/> once <see cref="ICommitGateSession.BeginTransactionAsync"/> is called and
    /// reports no pending work by default - mirroring a real store session closely enough for the driver's
    /// control flow to be exercised realistically, while staying a mock the test can assert calls against.
    /// </summary>
    private static ICommitGateSession CreateSession()
    {
        var session = Substitute.For<ICommitGateSession>();
        var hasActiveTransaction = false;
        session.HasActiveTransaction.Returns(_ => hasActiveTransaction);
        session
            .BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                hasActiveTransaction = true;
                return Task.CompletedTask;
            });
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(false);
        session.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        session.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return session;
    }

    private static IOutcomeClassifier ClassifierReturning(bool isFailure)
    {
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(isFailure);
        return classifier;
    }

    private sealed record PlainRequest : ICommand<Outcome<Success, Failure>>;

    private sealed record TransactionalRequest : ICommand<Outcome<Success, Failure>>, ITransactionalRequest;

    private sealed record ExternalStoreRequest : ICommand<Outcome<Success, Failure>>, IExternalStoreRequest;
}
