namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Proves that <see cref="EntityFrameworkUnitOfWork{TContext}"/> and
/// <see cref="EntityFrameworkRepository{TEntity,TKey,TContext}"/>, resolved from the same <c>Lazy&lt;TContext&gt;</c>,
/// participate in the same <see cref="DbContext"/> instance that
/// <c>Interlocutor.EntityFrameworkCore.TransactionBehavior{TRequest,TResponse,TContext}</c> sees for the same
/// dispatch - the "same transaction and session" guarantee this package's <see cref="IUnitOfWork"/> façade
/// relies on rather than re-implementing.
/// </summary>
public sealed class SharedSessionIntegrationTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;
    private readonly Lazy<TestDbContext> lazyContext;

    public SharedSessionIntegrationTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();
        this.lazyContext = new Lazy<TestDbContext>(() => this.context);
    }

    [Fact]
    public async Task RepositoryInsert_CommittedByTransactionBehavior_UsingSameLazyContext()
    {
        // Arrange
        var repository = new EntityFrameworkRepository<Widget, int, TestDbContext>(this.lazyContext, w => w.Id);
        var behavior = new Interlocutor.EntityFrameworkCore.TransactionBehavior<
            TransactionalRequest,
            OperationOutcome<Success, Failure>,
            TestDbContext
        >(this.lazyContext);

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(),
            () =>
            {
                // The repository resolves DbContext.Value from the exact same Lazy<TestDbContext> instance
                // TransactionBehavior itself resolves - proving both see one shared scoped context.
                repository.Query.ToList();
                this.context.Widgets.Add(new Widget { Name = "shared-session" });
                return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
            },
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);

        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["shared-session"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task UnitOfWorkSaveChanges_PersistsRepositoryInsert_WithinSameDispatch()
    {
        // Arrange
        var repository = new EntityFrameworkRepository<Widget, int, TestDbContext>(this.lazyContext, w => w.Id);
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(this.lazyContext);

        // Act
        await repository.InsertAsync(new Widget { Name = "via-unit-of-work" }, TestContext.Current.CancellationToken);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Reading through the same in-process context (not a fresh one) confirms the insert and the save
        // ran against the identical DbContext instance the Lazy<TestDbContext> hands out.
        Assert.Equal(["via-unit-of-work"], this.context.Widgets.Select(w => w.Name));
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private TestDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        return new TestDbContext(options);
    }

    private sealed record TransactionalRequest : ICommand<OperationOutcome<Success, Failure>>, ITransactionalRequest;

    public sealed class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => this.Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }
}
