namespace TodoTasks.Api.Tests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Persistence;
using TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves <see cref="TodoTasksDbContext"/>'s <see cref="User"/> mapping actually round-trips through a
/// real in-memory SQLite database - the kind of mapping mistake (a wrong column name, a missing
/// <c>IsRequired()</c>, a unique index that didn't actually take effect) <c>Unit/Features/Users/*</c>'s
/// NSubstitute-mocked handler tests can never catch.
/// </summary>
/// <remarks>
/// Boots the real host via <see cref="TodoTasksApiFactory"/> (<c>Program.cs</c>'s own DI wiring), exactly
/// like <c>InMemorySqliteConnectionPersistenceTests</c>, rather than reconstructing the connection/DbContext
/// registration as a separate, test-only copy.
/// </remarks>
public sealed class UserPersistenceTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public UserPersistenceTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task SaveThenGet_AUser_RoundTripsEveryMappedMember_Test()
    {
        // Arrange
        using var writeScope = this.factory.Services.CreateScope();
        using var readScope = this.factory.Services.CreateScope();
        var user = new User("Ada Lovelace", "ada@example.com");

        // Act - insert and save in one scope's DbContext, then read back through a second scope's
        // DbContext opened against the same kept-alive connection, the same way a real dispatch's insert
        // and a later request's read are two different scoped DbContext instances sharing one physical
        // database.
        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await writeContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var reloaded = await readContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(user.Id, reloaded.Id);
        Assert.Equal("Ada Lovelace", reloaded.DisplayName);
        Assert.Equal("ada@example.com", reloaded.Email);
    }

    [Fact]
    public async Task SaveChanges_TwoUsersWithTheSameEmail_ViolatesTheUniqueIndex_Test()
    {
        // Arrange - this exercises the schema-level unique index OnModelCreating declares directly,
        // bypassing CreateUserHandler's own pre-check query entirely, to prove the index itself is real
        // rather than merely assumed.
        using var scope = this.factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        await context.Users.AddAsync(new User("First User", "duplicate@example.com"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Users.AddAsync(new User("Second User", "duplicate@example.com"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
