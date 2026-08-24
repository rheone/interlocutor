namespace Interlocutor.EntityFrameworkCore.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Covers the commit/rollback decision logic of <see cref="TransactionBehavior{TRequest,TResponse,TContext}"/>:
/// classified success commits and persists, classified failure rolls back without a second
/// <c>SaveChangesAsync</c> call, and an exception from <c>SaveChangesAsync</c> rolls back and rethrows
/// unwrapped. These paths need a real ADO.NET transaction to observe commit/rollback effects, so this
/// suite runs against an open, shared Sqlite in-memory connection rather than mocking <see cref="DbContext"/>.
/// </summary>
public sealed class TransactionBehaviorCommitRollbackTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;

    public TransactionBehaviorCommitRollbackTests()
    {
        // A single open connection is required to keep a Sqlite ":memory:" database alive across the
        // context's lifetime - closing the last connection to it discards the database.
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();
    }

    [Fact]
    public async Task ClassifiedSuccess_CommitsTransaction_AndPersistsViaSaveChanges()
    {
        // Arrange
        var behavior = this.CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(1),
            () =>
            {
                this.context.Widgets.Add(new Widget { Name = "committed" });
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);

        // Persistence is verified via a fresh context reading through the same connection, so the assertion
        // only passes if SaveChangesAsync actually wrote the row and the transaction committed it.
        await using var verifyContext = this.CreateVerificationContext();
        Assert.Equal(["committed"], verifyContext.Widgets.Select(w => w.Name).ToArray());
    }

    [Fact]
    public async Task ClassifiedFailure_RollsBack_WithoutSecondSaveChangesCall_AndReturnsResultUnchanged()
    {
        // Arrange
        var behavior = this.CreateBehavior();
        var saveChangesCallCount = 0;
        this.context.SaveChangesCalled += () => saveChangesCallCount++;

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(2),
            () =>
            {
                // The handler tracks a change but never gets the chance to call SaveChangesAsync itself -
                // TransactionBehavior only calls it on the success path, so this change must never persist.
                this.context.Widgets.Add(new Widget { Name = "never-persisted" });
                return Task.FromResult<OperationOutcome<Success, Failure>>(new Failure("rejected"));
            },
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        var failure = Assert.IsType<Failure>(result.Value);
        Assert.Equal("rejected", failure.Reason);
        Assert.Equal(0, saveChangesCallCount);

        await using var verifyContext = this.CreateVerificationContext();
        Assert.Empty(verifyContext.Widgets);
    }

    [Fact]
    public async Task SaveChangesThrowing_RollsBackTransaction_AndRethrowsOriginalException()
    {
        // Arrange
        var thrown = new InvalidOperationException("boom from SaveChangesAsync");
        var behavior = this.CreateBehavior();
        this.context.ThrowOnSaveChanges = thrown;

        // Act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TransactionalRequest(3),
                () =>
                {
                    this.context.Widgets.Add(new Widget { Name = "rolled-back" });
                    return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
                },
                CancellationToken.None
            )
        );

        // Assert
        // Same exception instance, not a wrapper (e.g. not a DbUpdateException or AggregateException).
        Assert.Same(thrown, actual);

        await using var verifyContext = this.CreateVerificationContext();
        Assert.Empty(verifyContext.Widgets);
    }

    // Auto Generated, verify expected behavior:
    // Fills a gap the other three tests leave open: they all exercise the *default* classifier, whose
    // IsFailure decision happens to align with the OperationOutcome's own Success/Failure shape. This test
    // proves TransactionBehavior<,,> actually asks the injected IOutcomeClassifier - not the outcome's own
    // shape - by stubbing a classifier that reports failure for a result that is itself Success-shaped, and
    // confirming that drives a rollback (no persisted row) rather than the commit the outcome shape alone
    // would suggest. Mirrors Interlocutor.NHibernate.Tests.TransactionBehaviorTests, whose
    // Handle_RollsBackSession_WhenClassifierReportsFailure test documents the same classifier-authority
    // contract for the NHibernate sibling of this behavior.
    [Fact]
    public async Task Handle_UsesInjectedClassifier_NotOutcomeShape_ToDecideCommitVersusRollback()
    {
        // Arrange
        var classifier = Substitute.For<IOutcomeClassifier>();
        classifier.IsFailure(Arg.Any<object?>()).Returns(true);
        var behavior = new TransactionBehavior<TransactionalRequest, OperationOutcome<Success, Failure>, TestDbContext>(
            new Lazy<TestDbContext>(() => this.context),
            classifier
        );

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(4),
            () =>
            {
                this.context.Widgets.Add(new Widget { Name = "should-not-persist" });
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        // The outcome itself is still Success-shaped ...
        Assert.True(result.IsSuccess);

        // ... but the injected classifier said "failure", so TransactionBehavior rolled back instead of
        // committing and calling SaveChangesAsync - proving the classifier, not the outcome shape, drives
        // the decision.
        await using var verifyContext = this.CreateVerificationContext();
        Assert.Empty(verifyContext.Widgets);
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private TransactionBehavior<TransactionalRequest, OperationOutcome<Success, Failure>, TestDbContext> CreateBehavior() =>
        new(new Lazy<TestDbContext>(() => this.context));

    private TestDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        return new TestDbContext(options);
    }

    private sealed record TransactionalRequest(int Value) : ICommand<OperationOutcome<Success, Failure>>, ITransactionalRequest;

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        /// <summary>
        /// Raised each time <see cref="SaveChangesAsync(CancellationToken)"/> is invoked, so tests can assert
        /// it was called exactly the expected number of times without relying on side effects alone.
        /// </summary>
        public event Action? SaveChangesCalled;

        public DbSet<Widget> Widgets => this.Set<Widget>();

        /// <summary>
        /// Gets or sets an exception that, when set, the next call to <see cref="SaveChangesAsync(CancellationToken)"/>
        /// throws instead of persisting changes.
        /// </summary>
        public Exception? ThrowOnSaveChanges { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.SaveChangesCalled?.Invoke();

            if (this.ThrowOnSaveChanges is { } exception)
            {
                throw exception;
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }

    private sealed class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
