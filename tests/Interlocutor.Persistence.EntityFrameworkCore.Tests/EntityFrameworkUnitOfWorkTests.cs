namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Microsoft.Data.Sqlite;

/// <summary>
/// Covers <see cref="EntityFrameworkUnitOfWork{TContext}"/> in isolation from
/// <see cref="EntityFrameworkRepository{TEntity,TKey,TContext}"/>, against a real Sqlite in-memory database.
/// </summary>
// Auto Generated, verify expected behavior: closes the missing-test-file gap for EntityFrameworkUnitOfWork -
// its SaveChangesAsync was previously exercised only incidentally inside repository/shared-session tests.
public sealed class EntityFrameworkUnitOfWorkTests : IClassFixture<EntityFrameworkTestFixture>, IDisposable
{
    private readonly EntityFrameworkTestFixture fixture;
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;

    public EntityFrameworkUnitOfWorkTests(EntityFrameworkTestFixture fixture)
    {
        this.fixture = fixture;
        (this.connection, this.context) = fixture.CreateDatabase();
    }

    [Fact]
    public async Task SaveChangesAsync_ForwardsToContext_PersistingPendingChanges()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context));
        await this.context.Widgets.AddAsync(new Widget { Name = "pending" }, TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task CancelAsync_RollsBackOpenTransaction_AndClearsChangeTracker()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context));
        await this.context.Widgets.AddAsync(new Widget { Name = "cancelled" }, TestContext.Current.CancellationToken);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await unitOfWork.CancelAsync(TestContext.Current.CancellationToken);

        // Assert - the transaction was rolled back and the tracker cleared, so nothing is pending or persisted.
        Assert.Null(this.context.Database.CurrentTransaction);
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task Cancel_ForwardsToCancelAsync()
    {
        // Arrange
        var unitOfWork = new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context));
        await this.context.Widgets.AddAsync(new Widget { Name = "cancelled" }, TestContext.Current.CancellationToken);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - calling the superseded member must still roll back, just like CancelAsync.
        await unitOfWork.Cancel(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(this.context.Database.CurrentTransaction);
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private TestDbContext CreateVerificationContext() => this.fixture.CreateContext(this.connection);
}
