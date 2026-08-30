namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Covers <see cref="UnitOfWorkSaveChangesInterceptor"/> in isolation from <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>:
/// it must open a transaction ahead of a raw, direct <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// call - the fully-direct <c>Lazy&lt;TContext&gt;</c> access path neither the behavior nor
/// <see cref="EntityFrameworkUnitOfWork{TContext}"/> ever sees - unless suppressed for an
/// <c>IExternalStoreRequest</c> dispatch.
/// </summary>
public sealed class UnitOfWorkSaveChangesInterceptorTests : IDisposable
{
    private readonly SqliteConnection connection;

    public UnitOfWorkSaveChangesInterceptorTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
    }

    [Fact]
    public async Task SaveChangesAsync_WithInterceptorRegistered_LeavesATransactionOpen_InsteadOfAutoCommitting()
    {
        // Arrange
        await using var context = this.CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Act
        // A raw, direct call - bypassing IUnitOfWork and any pipeline behavior entirely.
        await context.Widgets.AddAsync(new Widget { Name = "direct" }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Without the interceptor, EF Core wraps a transaction-less SaveChanges call in its own implicit,
        // per-call transaction that commits immediately, leaving CurrentTransaction null afterward. The
        // interceptor instead opens an ambient transaction beforehand, which nothing here has committed, so
        // it is still open - proof the write happened inside a transaction this test (standing in for
        // UnitOfWorkBehavior) still controls.
        Assert.NotNull(context.Database.CurrentTransaction);

        await context.Database.CurrentTransaction.CommitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SaveChangesAsync_WhileSuppressed_NeverOpensATransaction()
    {
        // Arrange
        await using var context = this.CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        UnitOfWorkSaveChangesInterceptor.IsSuppressed.Value = true;
        try
        {
            // Act
            await context.Widgets.AddAsync(new Widget { Name = "external-store" }, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            // Suppressed (as UnitOfWorkBehavior does for the duration of an IExternalStoreRequest handler) -
            // the interceptor must not open a local transaction this request explicitly opted out of.
            Assert.Null(context.Database.CurrentTransaction);
        }
        finally
        {
            UnitOfWorkSaveChangesInterceptor.IsSuppressed.Value = false;
        }
    }

    public void Dispose() => this.connection.Dispose();

    private TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(this.connection)
            .AddInterceptors(new UnitOfWorkSaveChangesInterceptor())
            .Options;
        return new TestDbContext(options);
    }

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
