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
        Assert.True(result.IsSuccess);
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
        Assert.True(result.IsSuccess);
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // Committing flushes the pending insert and makes it durable - a real side effect a rollback
        // never produces, since no INSERT is ever sent in that path (see the failure test below).
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
        Assert.True(result.IsSuccess);
        this.AssertPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_ClassifiedFailure_RollsBack_NotCommit()
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
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return new Failure("rejected");
            },
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // Rolling back never flushes the pending insert - FlushMode.Commit means nothing auto-flushes
        // beforehand, and RollbackAsync doesn't flush either - so the row never becomes durable.
        this.AssertNotPersisted(widget.Id);
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
        Assert.True(result.IsSuccess);
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        Assert.False(await session.IsDirtyAsync(CancellationToken.None));
        Assert.False(session.Contains(widget)); // Session.Clear() evicted the widget entirely.

        // SaveChangesAsync's own flush already sent the insert to the database before Cancel rolled it
        // back - the strongest proof Cancel undid it for real, not just in this session's own tracking.
        this.AssertNotPersisted(widget.Id);
    }

    [Fact]
    public async Task HandleAsync_UsesInjectedClassifier_NotOutcomeShape_ToDecideCommitVersusRollback()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(true);
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
        Assert.True(result.IsSuccess);
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        // The classifier says "failure" despite the Success payload, so the behavior rolls back - proving
        // the classifier drives the decision, not the outcome's own shape. Same never-flushed proof as
        // HandleAsync_ClassifiedFailure_RollsBack_NotCommit.
        this.AssertNotPersisted(widget.Id);
    }

    [Fact]
    public async Task Handle_ForwardsToHandleAsync()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var behavior = CreateBehavior(session);
        var widget = new Widget();

        // Act - calling the superseded member must still commit a real write, just like HandleAsync.
        var result = await behavior.Handle(
            new PlainRequest(),
            async () =>
            {
                await session.SaveAsync(widget, CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
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
