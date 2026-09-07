namespace Interlocutor.Persistence.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Covers <see cref="CommitGateDriver"/>'s shared begin/run-next/check-pending-work/commit-or-rollback
/// algorithm directly, against a mocked <see cref="ICommitGateSession"/> and a mocked <c>next()</c> delegate,
/// per <c>documentation/testing.md</c>'s "mock the <c>next()</c> delegate" guidance - so the algorithm itself
/// is verified once at the seam that owns it, rather than only indirectly through each store's own
/// <c>UnitOfWorkBehaviorTests</c>. Rollback via the ambient <c>IUnitOfWork.CancelAsync</c> signal is not
/// decided by this algorithm at all - see <c>UnitOfWorkBehaviorTests</c>' own coverage of that call, which
/// rolls back before this driver's pending-work check ever runs. Rollback via the call-site
/// <see cref="NoCommit"/> signal, by contrast, is decided here - see the <c>NoCommit</c>-prefixed tests below.
/// </summary>
public sealed class CommitGateDriverTests
{
    [Fact]
    public async Task HandleAsync_ExternalStoreRequest_NeverInvokesSessionFactory()
    {
        // Arrange
        var sessionFactoryInvoked = false;
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new ExternalStoreRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () =>
            {
                sessionFactoryInvoked = true;
                return Substitute.For<ICommitGateSession>();
            },
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));

        // The bypass guarantee: a request that opts out of local transaction management never pays for
        // resolving the underlying store session at all - see CommitGateDriver's own remarks.
        Assert.False(sessionFactoryInvoked);
    }

    [Fact]
    public async Task HandleAsync_TransactionalRequest_BeginsTransactionEagerly_BeforeNextRuns()
    {
        // Arrange
        var session = CreateSession();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();
        var transactionActiveBeforeNext = false;

        // Act
        await CommitGateDriver.HandleAsync(
            new TransactionalRequest(),
            () =>
            {
                transactionActiveBeforeNext = session.HasActiveTransaction;
                return Task.FromResult<Outcome<Success, Failure>>(default(Success));
            },
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(transactionActiveBeforeNext);
        await session.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PendingWorkAfterNext_CommitsAndDoesNotRollBack()
    {
        // Arrange - no eager begin, but the session reports staged work once next() completes, matching a
        // write staged directly on the session with no transaction opened yet.
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        await session.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EagerTransactionWithNoPendingWork_StillCommits()
    {
        // Arrange - the eager-begin transaction opened for a check-then-act read still has to be closed even
        // though nothing ended up pending; commit is the only decision this driver ever makes for an active
        // transaction it reaches this point with - distinct from "nothing to finalize", which only applies
        // when no transaction was ever opened.
        var session = CreateSession();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new TransactionalRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(new Failure("rejected")),
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Failure _));
        await session.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());

        // Only the one eager begin - the post-next() check must not begin a second transaction on top of it.
        await session.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoCommitWith_RollsBackInsteadOfCommitting()
    {
        // Arrange - the ordinary-return half of the call-site "don't commit" signal: NoCommit.With returns
        // the handler's chosen value unchanged, but marks the dispatch to roll back instead of commit.
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult(NoCommit.With<Outcome<Success, Failure>>(default(Success))),
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert - the returned value is untouched by the signal; only the commit decision changes.
        Assert.True(result.TryGetValue(out Success _));
        await session.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoCommitAndEject_RollsBackInsteadOfCommitting()
    {
        // Arrange - the short-circuit half of the signal: NoCommit.AndEject throws PipelineEjectException,
        // exactly like Eject.With, so next() here mimics what PipelineChainExecutor's own eject guard does in
        // a real dispatch - converting it back into an ordinary response before CommitGateDriver ever sees it.
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () =>
            {
                try
                {
                    return Task.FromResult(NoCommit.AndEject<Outcome<Success, Failure>>(new Failure("rejected")));
                }
                catch (PipelineEjectException<Outcome<Success, Failure>> eject)
                {
                    return Task.FromResult(eject.Response);
                }
            },
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Failure _));
        await session.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoCommitRequestedButNothingPending_NeitherCommitsNorRollsBack()
    {
        // Arrange - symmetric with IUnitOfWork.Cancel's own no-op-when-nothing-pending behavior: signaling
        // no-commit alongside a dispatch that never actually wrote anything has nothing to finalize either way.
        var session = CreateSession();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult(NoCommit.With<Outcome<Success, Failure>>(default(Success))),
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        await session.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoCommitSignal_DoesNotLeakToTheNextDispatch()
    {
        // Arrange - NoCommit's ambient flag must be consumed (read-and-cleared) every dispatch, or a stray
        // no-commit request would silently roll back an unrelated, later dispatch on the same async flow.
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        var firstSession = CreateSession();
        firstSession.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);

        // Act - first dispatch signals no-commit ...
        await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult(NoCommit.With<Outcome<Success, Failure>>(default(Success))),
            () => firstSession,
            describer,
            logger,
            CancellationToken.None
        );

        // ... a second, unrelated dispatch on the same async flow signals nothing at all.
        var secondSession = CreateSession();
        secondSession.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);

        await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => secondSession,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert - the first dispatch's signal rolled back its own session only; the second committed as
        // normal, proving the flag was cleared in between rather than leaking forward.
        await firstSession.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await secondSession.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await secondSession.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoActiveTransactionAndNoPendingWork_NeitherCommitsNorRollsBack()
    {
        // Arrange - a pure read (or a dispatch that short-circuited before writing): nothing was ever staged,
        // so there is nothing to finalize at all.
        var session = CreateSession();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();

        // Act
        var result = await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
            describer,
            logger,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        await session.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await session.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NextThrows_PropagatesException_WithoutCommittingOrRollingBack()
    {
        // Arrange - the driver has no try/catch of its own around next(): an exception is a bug/infra fault,
        // not an outcome to classify, so it propagates unchanged and neither finalization path runs here.
        var session = CreateSession();
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        var logger = Substitute.For<ILogger>();
        var handlerException = new InvalidOperationException("handler blew up");

        // Act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CommitGateDriver.HandleAsync(
                new PlainRequest(),
                Task<Outcome<Success, Failure>> () => throw handlerException,
                () => session,
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
    public async Task HandleAsync_CommitPath_LogsDebugLine_WithRequestTypeAndDescribedOutcome()
    {
        // Arrange
        var session = CreateSession();
        session.HasPendingWorkAsync(Arg.Any<CancellationToken>()).Returns(true);
        var describer = Substitute.For<IResponseOutcomeDescriber>();
        describer.Describe(Arg.Any<object?>()).Returns("Success");
        var logger = Substitute.For<ILogger>();

        // Act
        await CommitGateDriver.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            () => session,
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

    private sealed record PlainRequest : ICommand<Outcome<Success, Failure>>;

    private sealed record TransactionalRequest : ICommand<Outcome<Success, Failure>>, ITransactionalRequest;

    private sealed record ExternalStoreRequest : ICommand<Outcome<Success, Failure>>, IExternalStoreRequest;
}
