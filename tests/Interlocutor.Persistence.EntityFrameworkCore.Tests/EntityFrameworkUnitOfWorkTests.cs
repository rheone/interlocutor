namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Covers <see cref="EntityFrameworkUnitOfWork{TContext}"/> in isolation from
/// <see cref="EntityFrameworkRepository{TEntity,TKey,TContext}"/>, against a real Sqlite in-memory database.
/// </summary>
// Auto Generated, verify expected behavior: closes the missing-test-file gap for EntityFrameworkUnitOfWork -
// its SaveChangesAsync was previously exercised only incidentally inside repository/shared-session tests.
public sealed class EntityFrameworkUnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;

    public EntityFrameworkUnitOfWorkTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_ForwardsToContext_PersistingPendingChanges()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context));
        this.context.Widgets.Add(new Widget { Name = "pending" });

        // Act
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verify = this.CreateVerificationContext();
        Assert.Equal(["pending"], verify.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoPendingChanges_CompletesWithoutPersistingAnything()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context));

        // Act
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
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
