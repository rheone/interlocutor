namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Covers <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s begin-on-first-write, eager-begin,
/// commit/rollback, and <see cref="IExternalStoreRequest"/> opt-out behavior per ADR-0018. Runs against a real,
/// open Sqlite in-memory connection (not a mocked <see cref="DbContext"/>) so transaction begin/commit/rollback
/// have real, observable effects.
/// </summary>
public sealed class UnitOfWorkBehaviorTests : IClassFixture<EntityFrameworkTestFixture>, IDisposable
{
    private readonly EntityFrameworkTestFixture fixture;
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;
    private readonly Lazy<TestDbContext> lazyContext;

    public UnitOfWorkBehaviorTests(EntityFrameworkTestFixture fixture)
    {
        this.fixture = fixture;
        (this.connection, this.context) = fixture.CreateDatabase();
        this.lazyContext = new Lazy<TestDbContext>(() => this.context);
    }

    [Fact]
    public async Task HandleAsync_DispatchThatNeverWrites_NeverOpensTransaction()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task HandleAsync_WriteStagedDirectlyOnContext_OpensTransactionAndCommitsOnSuccess()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                // Direct DbContext access, bypassing IUnitOfWork/IDomainRepository entirely - the escape hatch
                // ADR-0018 requires begin-on-first-write to cover too.
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "direct-write" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["direct-write"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task HandleAsync_NullPayloadSuccess_CommitsNotRollsBack()
    {
        // Arrange - a handler that legitimately succeeds with a null case payload (see finding 01) must
        // still commit, not roll back a genuine write as if it had failed.
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<string?, Failure>, TestDbContext>(this.lazyContext);

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "null-payload-success" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return (string?)null;
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out string? _));
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["null-payload-success"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task HandleAsync_NullPayloadSuccessRoutedThroughResourceCommandHandler_CommitsNotRollsBack()
    {
        // Arrange - the realistic trigger from finding 01: ResourceCommandHandler<TCommand, TEntity, TResponse>
        // (the library's own built-in resource-authorization template) lets a consumer pick a TResponse whose
        // success case is nullable, not just a bare hand-constructed Outcome<...>.
        var behavior = new UnitOfWorkBehavior<
            NullableSuccessResourceCommand,
            Outcome<string?, NotFound, Forbidden>,
            TestDbContext
        >(this.lazyContext);
        var resourceHandler = new NullableSuccessResourceCommandHandler(this.context);
        var command = new NullableSuccessResourceCommand();

        // Act
        var result = await behavior.HandleAsync(
            command,
            () => resourceHandler.HandleAsync(command, CancellationToken.None),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out string? _));
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["resource-handler-write"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task HandleAsync_FailureShapedOutcome_StillCommits_NoCaseTypeDecidesRollback()
    {
        // Arrange - no case type - Success, Failure, or otherwise - determines the commit decision; a
        // Failure-shaped outcome commits the same as a Success-shaped one unless the handler explicitly
        // signals otherwise via IUnitOfWork.CancelAsync (see the Cancel test below).
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "failure-shaped-still-commits" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return new Failure("rejected");
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Failure _));
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["failure-shaped-still-commits"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task HandleAsync_TransactionalRequest_BeginsEagerly_BeforeHandlerWrites()
    {
        // Arrange
        var behavior = new UnitOfWorkBehavior<TransactionalRequest, Outcome<Success, Failure>, TestDbContext>(this.lazyContext);
        var transactionOpenBeforeWrite = false;

        // Act
        await behavior.HandleAsync(
            new TransactionalRequest(),
            () =>
            {
                // Observed before any write happens - only an eager begin at pipeline start explains a
                // non-null CurrentTransaction at this point.
                transactionOpenBeforeWrite = this.context.Database.CurrentTransaction is not null;
                return Task.FromResult<Outcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(transactionOpenBeforeWrite);
    }

    [Fact]
    public async Task HandleAsync_ExternalStoreRequest_NeverOpensLocalTransaction_EvenWhenItWrites()
    {
        // Arrange
        var behavior = new UnitOfWorkBehavior<ExternalStoreRequest, Outcome<Success, Failure>, TestDbContext>(this.lazyContext);

        // Act
        var result = await behavior.HandleAsync(
            new ExternalStoreRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "external" }, CancellationToken.None)
                    .ConfigureAwait(false);
                // A raw SaveChangesAsync call, with no ambient transaction, auto-commits immediately under
                // EF Core's own per-call implicit transaction - proving this request's write really did skip
                // this behavior's local transaction machinery, not merely that nothing was staged.
                await this.context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task HandleAsync_UnitOfWorkCancel_RollsBackImmediately_AndPreservesHandlerOutcome()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(this.lazyContext);
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "cancelled" }, CancellationToken.None)
                    .ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

                // The handler cancels, then still returns its own Success outcome - Cancel is not a
                // short-circuit primitive, so the handler retains control of what it returns.
                await unitOfWork.CancelAsync(CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        // The outcome is unchanged - Cancel does not override what the handler returns.
        Assert.True(result.TryGetValue(out Success _));

        // But nothing was actually persisted: Cancel rolled back and cleared the change tracker, so the
        // behavior's end-of-pipeline check found nothing pending and never re-committed.
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task HandleAsync_HandlerThrowsAfterWrite_LeavesTransactionUncommitted_RolledBackByContextDisposal()
    {
        // Arrange - the write goes through IUnitOfWork.SaveChangesAsync (not a raw AddAsync left unsaved),
        // so it genuinely reaches the database inside an open transaction before the handler throws. This is
        // the strongest proof that what follows is a real store-level rollback on disposal, not merely "the
        // write was never staged" - CommitGateDriver has no try/catch of its own around next() (see the
        // CommitGateDriverTests.HandleAsync_NextThrows_... coverage), so neither commit nor rollback runs here.
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(this.lazyContext);
        var behavior = this.CreateBehavior();
        var thrown = new InvalidOperationException("handler blew up");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(
                new PlainRequest(),
                async () =>
                {
                    await this
                        .context.Widgets.AddAsync(new Widget { Name = "rolled-back-on-dispose" }, CancellationToken.None)
                        .ConfigureAwait(false);
                    await unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                    throw thrown;
                },
                CancellationToken.None
            )
        );

        // Assert - left open, neither committed nor rolled back by CommitGateDriver itself.
        Assert.NotNull(this.context.Database.CurrentTransaction);

        // Disposing the context - what a scoped DI container does once a failed dispatch unwinds - rolls
        // back the still-open transaction, per ADO.NET's own commit-or-dispose contract. This is the entire
        // guarantee the "implicit no-commit via exception" path now rests on.
        await this.context.DisposeAsync();

        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task HandleAsync_NoCommitWith_RollsBackWrite_NoCaseTypeInvolved()
    {
        // Arrange - the call-site "don't commit" signal from Interlocutor.Abstractions.Pipeline.NoCommit,
        // exercised end-to-end against a real transaction: the handler still returns its own Success outcome,
        // proving the signal is fully decoupled from the value it accompanies.
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "no-commit-signal" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return NoCommit.With<Outcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        Assert.Null(this.context.Database.CurrentTransaction);
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task HandleAsync_InjectedClassifier_NoLongerAffectsCommitDecision()
    {
        // Arrange - IOutcomeClassifier is a pure label registry (see OutcomeClassifierBuilder); it has no
        // failure judgment left to consult, so an injected classifier can no longer force a rollback - only
        // an explicit IUnitOfWork.CancelAsync (covered above) does that now.
        var classifier = Substitute.For<IOutcomeClassifier>();
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>, TestDbContext>(
            this.lazyContext,
            classifier
        );

        // Act
        var result = await behavior.HandleAsync(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "commits-regardless-of-classifier" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.TryGetValue(out Success _));
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["commits-regardless-of-classifier"], verify.Widgets.Select(w => w.Name));
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>, TestDbContext> CreateBehavior() =>
        new(this.lazyContext);

    private TestDbContext CreateVerificationContext() => this.fixture.CreateContext(this.connection);

    private sealed record PlainRequest : ICommand<Outcome<Success, Failure>>;

    private sealed record NullableSuccessResourceCommand : ICommand<Outcome<string?, NotFound, Forbidden>>;

    /// <summary>Loads/authorizes trivially, writes a <see cref="Widget"/>, and succeeds with a null payload.</summary>
    private sealed class NullableSuccessResourceCommandHandler(TestDbContext context)
        : ResourceCommandHandler<NullableSuccessResourceCommand, Widget, Outcome<string?, NotFound, Forbidden>>
    {
        protected override Task<Widget?> LoadAsync(NullableSuccessResourceCommand command, CancellationToken ct) =>
            Task.FromResult<Widget?>(new Widget());

        protected override Forbidden? Authorize(Widget entity, NullableSuccessResourceCommand command) => null;

        protected override async Task<Outcome<string?, NotFound, Forbidden>> HandleAsync(
            NullableSuccessResourceCommand command,
            Widget entity,
            CancellationToken ct
        )
        {
            await context.Widgets.AddAsync(new Widget { Name = "resource-handler-write" }, ct).ConfigureAwait(false);
            return (string?)null;
        }
    }

    private sealed record TransactionalRequest : ICommand<Outcome<Success, Failure>>, ITransactionalRequest;

    private sealed record ExternalStoreRequest : ICommand<Outcome<Success, Failure>>, IExternalStoreRequest;
}
