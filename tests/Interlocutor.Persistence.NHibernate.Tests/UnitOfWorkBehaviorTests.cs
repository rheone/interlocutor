namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Covers <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/>'s flush-mode default, begin-on-first-write,
/// eager-begin, commit/rollback, and <see cref="IExternalStoreRequest"/> opt-out behavior per ADR-0018,
/// against a real in-memory <see cref="ISession"/> (see <see cref="RealSessionFixture"/>) - the same style
/// as this project's other behavior coverage (see <see cref="SharedSessionIntegrationTests"/>'s remarks for
/// why <see cref="ISession"/> query execution itself is covered elsewhere instead).
/// </summary>
public sealed class UnitOfWorkBehaviorTests : IClassFixture<RealSessionFixture>
{
    private readonly RealSessionFixture fixture;

    public UnitOfWorkBehaviorTests(RealSessionFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task HandleAsync_DispatchThatNeverWrites_NeverBeginsTransaction()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.Null(session.GetCurrentTransaction());
    }

    [Fact]
    public async Task HandleAsync_SetsFlushModeToCommit_BeforeHandlerRuns()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);
        var flushModeWhenHandlerRan = FlushMode.Auto;

        // Act
        await behavior.HandleAsync(
            new PlainRequest(),
            () =>
            {
                flushModeWhenHandlerRan = session.FlushMode;
                return Task.FromResult<Outcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        // Prevents NHibernate's default auto-flush-before-query from writing staged changes ahead of any
        // transaction this behavior opens - see this type's remarks.
        Assert.Equal(FlushMode.Commit, flushModeWhenHandlerRan);
    }

    [Fact]
    public async Task HandleAsync_WriteStagedDirectlyOnSession_BeginsTransactionAndCommitsOnSuccess()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                // A direct Save() on the session, not through NHibernateUnitOfWork - IsDirty() reflects
                // the session's own tracked state regardless of which access path staged it.
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // Committing flushes the pending insert and makes it durable.
        this.AssertPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_NullPayloadSuccess_CommitsNotRollsBack()
    {
        // Arrange - a handler that legitimately succeeds with a null case payload (see finding 01) must
        // still commit, not roll back a genuine write as if it had failed.
        using var session = this.fixture.OpenSession();
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<string?, Failure>>(new Lazy<ISession>(() => session));
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return (string?)null;
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out string? _));
        this.AssertPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_FailureShapedOutcome_StillCommits_NoCaseTypeDecidesRollback()
    {
        // Arrange - no case type - Success, Failure, or otherwise - determines the commit decision; a
        // Failure-shaped outcome commits the same as a Success-shaped one unless the handler explicitly
        // signals otherwise via IUnitOfWork.CancelAsync (see the Cancel test below).
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return new Failure("rejected");
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Failure _));
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // Committing flushes the pending insert and makes it durable, despite the Failure-shaped outcome.
        this.AssertPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_TransactionalRequest_BeginsEagerly_BeforeHandlerWrites()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var behavior = new UnitOfWorkBehavior<TransactionalRequest, Outcome<Success, Failure>>(
            new Lazy<ISession>(() => session)
        );
        var transactionActiveBeforeHandler = false;

        // Act
        await behavior.HandleAsync(
            new TransactionalRequest(),
            () =>
            {
                transactionActiveBeforeHandler = session.GetCurrentTransaction() is { IsActive: true };
                return Task.FromResult<Outcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(transactionActiveBeforeHandler);
    }

    [Fact]
    public async Task HandleAsync_ExternalStoreRequest_SkipsSession_EvenThoughAlsoTransactional()
    {
        // Arrange
        var session = new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved"));
        var behavior = new UnitOfWorkBehavior<RequestClaimingBothMarkers, Outcome<string, Failure>>(session);
        var handled = false;

        // Act
        var result = await behavior.HandleAsync(
            new RequestClaimingBothMarkers(),
            () =>
            {
                handled = true;
                return Task.FromResult<Outcome<string, Failure>>("done");
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(handled);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task HandleAsync_UnitOfWorkCancel_RollsBackImmediately_AndPreservesHandlerOutcome()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        var behavior = CreateBehavior(session);
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

                // The handler cancels, then still returns its own Success outcome - Cancel is not a
                // short-circuit primitive, so the handler retains control of what it returns.
                await unitOfWork.CancelAsync(CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        Assert.False(await session.IsDirtyAsync(CancellationToken.None));
        Assert.False(session.Contains(widget)); // Session.Clear() evicted the widget entirely.

        // SaveChangesAsync's own flush already sent the insert to the database before Cancel rolled it
        // back - the strongest proof Cancel undid it for real, not just in this session's own tracking.
        this.AssertNotPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_HandlerThrowsAfterWrite_LeavesTransactionUncommitted_RolledBackBySessionDisposal()
    {
        // Arrange - the write goes through IUnitOfWork.SaveChangesAsync (not a raw Save() left unflushed), so
        // it genuinely reaches the database inside an open transaction before the handler throws - the
        // strongest proof that what follows is a real store-level rollback on disposal, not merely "the
        // write was never staged". CommitGateDriver has no try/catch of its own around next(), so neither
        // commit nor rollback runs here.
        //
        // Not a `using` declaration: disposal must happen mid-test, before the post-dispose assertion below,
        // not at method exit - this is the one place in this suite that deliberately proves disposal itself
        // rolls back an open transaction, so the explicit Dispose() call is the point of the test.
#pragma warning disable IDISP017
        var session = this.fixture.OpenSession();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        var behavior = CreateBehavior(session);
        var widget = new Widget();
        var thrown = new InvalidOperationException("handler blew up");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(
                new PlainRequest(),
                async () =>
                {
                    await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                    await unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                    throw thrown;
                },
                CancellationToken.None
            )
        );

        // Assert - left open, neither committed nor rolled back by CommitGateDriver itself.
        Assert.True(session.GetCurrentTransaction() is { IsActive: true });

        // Disposing the session - what a scoped DI container does once a failed dispatch unwinds - rolls
        // back the still-open transaction (NHibernate's ITransaction rolls back on disposal if never
        // committed). This is the entire guarantee the "implicit no-commit via exception" path now rests on.
        session.Dispose();
#pragma warning restore IDISP017

        this.AssertNotPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_NoCommitWith_RollsBackWrite_NoCaseTypeInvolved()
    {
        // Arrange - the call-site "don't commit" signal from Interlocutor.Abstractions.Pipeline.NoCommit,
        // exercised end-to-end against a real transaction: the handler still returns its own Success outcome,
        // proving the signal is fully decoupled from the value it accompanies.
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return NoCommit.With<Outcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        this.AssertNotPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_InjectedClassifier_NoLongerAffectsCommitDecision()
    {
        // Arrange - IOutcomeClassifier is a pure label registry (see OutcomeClassifierBuilder); it has no
        // failure judgment left to consult, so an injected classifier can no longer force a rollback - only
        // an explicit IUnitOfWork.CancelAsync (covered above) does that now.
        using var session = this.fixture.OpenSession();
        var classifier = Substitute.For<IOutcomeClassifier>();
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>>(
            new Lazy<ISession>(() => session),
            classifier
        );
        var widget = new Widget();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        this.AssertPersisted(widget.Id);
    }

    /// <summary>Opens a fresh session and asserts a <see cref="Widget"/> with the given id was actually committed.</summary>
    private void AssertPersisted(Guid widgetId)
    {
        using var verifySession = this.fixture.OpenSession();
        Assert.NotNull(verifySession.Get<Widget>(widgetId));
    }

    /// <summary>Opens a fresh session and asserts no <see cref="Widget"/> with the given id was ever committed.</summary>
    private void AssertNotPersisted(Guid widgetId)
    {
        using var verifySession = this.fixture.OpenSession();
        Assert.Null(verifySession.Get<Widget>(widgetId));
    }

    private static UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>> CreateBehavior(ISession session) =>
        new(new Lazy<ISession>(() => session));

    private sealed record PlainRequest : ICommand<Outcome<Success, Failure>>;

    private sealed record TransactionalRequest : ICommand<Outcome<Success, Failure>>, ITransactionalRequest;

    private sealed record RequestClaimingBothMarkers
        : ICommand<Outcome<string, Failure>>,
            ITransactionalRequest,
            IExternalStoreRequest;
}
