namespace TodoTasks.Api.Tests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Persistence;
using TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves <see cref="TodoTasksDbContext"/>'s <see cref="TodoTask"/> mapping actually round-trips
/// through a real in-memory SQLite database - the kind of mapping mistake (a wrong column name, a
/// missing <c>IsRequired()</c>, an <c>OwnerId</c> that accidentally became a foreign key, a unique
/// index that didn't actually take effect or that isn't scoped the way it should be)
/// <c>Unit/Features/Tasks/CreateTodoTask/*</c>'s NSubstitute-mocked handler tests can never catch.
/// </summary>
/// <remarks>
/// Boots the real host via <see cref="TodoTasksApiFactory"/> (<c>Program.cs</c>'s own DI wiring), exactly
/// like <c>UserPersistenceTests</c>, rather than reconstructing the connection/DbContext registration as
/// a separate, test-only copy.
/// </remarks>
public sealed class TodoTaskPersistenceTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public TodoTaskPersistenceTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task SaveThenGet_ATodoTask_RoundTripsEveryMappedMember_Test()
    {
        // Arrange
        using var writeScope = this.factory.Services.CreateScope();
        using var readScope = this.factory.Services.CreateScope();
        var task = new TodoTask("Buy milk", "owner-1", "2%, please");

        // Act - insert and save in one scope's DbContext, then read back through a second scope's
        // DbContext opened against the same kept-alive connection, the same way a real dispatch's insert
        // and a later request's read are two different scoped DbContext instances sharing one physical
        // database.
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await writeContext.Tasks.AddAsync(task, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(task.Id, reloaded.Id);
        Assert.Equal("Buy milk", reloaded.Title);
        Assert.Equal("2%, please", reloaded.Notes);
        Assert.Equal("owner-1", reloaded.OwnerId);
        Assert.False(reloaded.IsCompleted);
        Assert.Equal(task.CreatedAtUtc, reloaded.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChanges_TwoTasksWithTheSameTitleForTheSameOwner_ViolatesTheUniqueIndex_Test()
    {
        // Arrange - this exercises the schema-level unique index OnModelCreating declares directly on
        // (OwnerId, Title), bypassing CreateTodoTaskHandler's own pre-check query entirely, to prove the
        // index itself is real rather than merely assumed.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await context.Tasks.AddAsync(new TodoTask("Duplicate title", "owner-1", null), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Tasks.AddAsync(new TodoTask("Duplicate title", "owner-1", null), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_TwoTasksWithTheSameTitleForDifferentOwners_Succeeds_Test()
    {
        // Arrange - the unique index is scoped to (OwnerId, Title), not Title alone: two different
        // owners must each be free to have their own "Buy milk", matching CreateTodoTaskHandler's own
        // per-owner duplicate-title semantics rather than a global one.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await context.Tasks.AddAsync(new TodoTask("Shared title", "owner-1", null), TestContext.Current.CancellationToken);
        await context.Tasks.AddAsync(new TodoTask("Shared title", "owner-2", null), TestContext.Current.CancellationToken);

        // Act
        var savedCount = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, savedCount);
    }
}
