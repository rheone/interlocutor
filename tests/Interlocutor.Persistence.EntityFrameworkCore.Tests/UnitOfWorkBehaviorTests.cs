namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
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
public sealed class UnitOfWorkBehaviorTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;
    private readonly Lazy<TestDbContext> lazyContext;

    public UnitOfWorkBehaviorTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();
        this.lazyContext = new Lazy<TestDbContext>(() => this.context);
    }

    [Fact]
    public async Task Handle_DispatchThatNeverWrites_NeverOpensTransaction()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            () => Task.FromResult<Outcome<Success, Failure>>(default(Success)),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Handle_WriteStagedDirectlyOnContext_OpensTransactionAndCommitsOnSuccess()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.Handle(
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
        Assert.True(result.IsSuccess);
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["direct-write"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task Handle_NullPayloadSuccess_CommitsNotRollsBack()
    {
        // Arrange - a handler that legitimately succeeds with a null case payload (see finding 01) must
        // still commit, not roll back a genuine write as if it had failed.
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<string?, Failure>, TestDbContext>(this.lazyContext);

        // Act
        var result = await behavior.Handle(
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
        Assert.True(result.IsSuccess);
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["null-payload-success"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task Handle_NullPayloadSuccessRoutedThroughResourceCommandHandler_CommitsNotRollsBack()
    {
        // Arrange - the realistic trigger from finding 01: ResourceCommandHandler<TCommand, TEntity, TSuccess>
        // (the library's own built-in resource-authorization template) lets a consumer pick a nullable
        // TSuccess, not just a bare hand-constructed Outcome<...>.
        var behavior = new UnitOfWorkBehavior<
            NullableSuccessResourceCommand,
            Outcome<string?, NotFound, Forbidden>,
            TestDbContext
        >(this.lazyContext);
        var resourceHandler = new NullableSuccessResourceCommandHandler(this.context);
        var command = new NullableSuccessResourceCommand();

        // Act
        var result = await behavior.Handle(
            command,
            () => resourceHandler.Handle(command, CancellationToken.None),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["resource-handler-write"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task Handle_ClassifiedFailure_RollsBack_NoRowPersisted()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "never-persisted" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return new Failure("rejected");
            },
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_BeginsEagerly_BeforeHandlerWrites()
    {
        // Arrange
        var behavior = new UnitOfWorkBehavior<TransactionalRequest, Outcome<Success, Failure>, TestDbContext>(this.lazyContext);
        var transactionOpenBeforeWrite = false;

        // Act
        await behavior.Handle(
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
    public async Task Handle_ExternalStoreRequest_NeverOpensLocalTransaction_EvenWhenItWrites()
    {
        // Arrange
        var behavior = new UnitOfWorkBehavior<ExternalStoreRequest, Outcome<Success, Failure>, TestDbContext>(this.lazyContext);

        // Act
        var result = await behavior.Handle(
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
        Assert.True(result.IsSuccess);
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Handle_UnitOfWorkCancel_RollsBackImmediately_AndPreservesHandlerOutcome()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(this.lazyContext);
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "cancelled" }, CancellationToken.None)
                    .ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

                // The handler cancels, then still returns its own Success outcome - Cancel is not a
                // short-circuit primitive, so the handler retains control of what it returns.
                await unitOfWork.Cancel(CancellationToken.None).ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        // The outcome is unchanged - Cancel does not override what the handler returns.
        Assert.True(result.IsSuccess);

        // But nothing was actually persisted: Cancel rolled back and cleared the change tracker, so the
        // behavior's end-of-pipeline check found nothing pending and never re-committed.
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
        Assert.Null(this.context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Handle_UsesInjectedClassifier_NotOutcomeShape_ToDecideCommitVersusRollback()
    {
        // Arrange
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(true);
        var behavior = new UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>, TestDbContext>(
            this.lazyContext,
            classifier
        );

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            async () =>
            {
                await this
                    .context.Widgets.AddAsync(new Widget { Name = "should-not-persist" }, CancellationToken.None)
                    .ConfigureAwait(false);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert
        // The outcome itself is still Success-shaped ...
        Assert.True(result.IsSuccess);

        // ... but the injected classifier said "failure", so the behavior rolled back instead of committing.
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private UnitOfWorkBehavior<PlainRequest, Outcome<Success, Failure>, TestDbContext> CreateBehavior() =>
        new(this.lazyContext);

    private TestDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        return new TestDbContext(options);
    }

    private sealed record PlainRequest : ICommand<Outcome<Success, Failure>>;

    private sealed record NullableSuccessResourceCommand : ICommand<Outcome<string?, NotFound, Forbidden>>;

    /// <summary>Loads/authorizes trivially, writes a <see cref="Widget"/>, and succeeds with a null payload.</summary>
    private sealed class NullableSuccessResourceCommandHandler(TestDbContext context)
        : ResourceCommandHandler<NullableSuccessResourceCommand, Widget, string?>
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

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => this.Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }

    private sealed class Widget
    {
        // Never read directly by this test's assertions, but EF Core's key-by-convention requires an
        // Id property to treat this type as having a primary key at all - it's structural, not dead.
#pragma warning disable S1144
        public int Id { get; set; }
#pragma warning restore S1144

        public string Name { get; set; } = string.Empty;
    }
}
