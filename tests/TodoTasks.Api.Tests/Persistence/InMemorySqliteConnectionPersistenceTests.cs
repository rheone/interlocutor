namespace TodoTasks.Api.Tests.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoTasks.Api.Persistence;
using TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves the real risk in <c>Program.cs</c>'s in-memory SQLite connection strategy: a single
/// <c>SqliteConnection</c>, opened once and registered as a singleton, is what keeps the in-memory
/// database alive and visible across the many independent, scoped <see cref="TodoTasksDbContext"/>
/// instances the app hands out per request - not just within one <see cref="TodoTasksDbContext"/>
/// instance, and not just within one DI scope.
/// </summary>
/// <remarks>
/// This deliberately boots the real host via <see cref="TodoTasksApiFactory"/> (<c>Program.cs</c>'s own
/// DI wiring) rather than reconstructing the connection/DbContext registration as a separate,
/// test-only copy - a wiring mistake in <c>Program.cs</c> itself (for example, resolving a fresh
/// <c>SqliteConnection</c> per scope instead of the shared singleton) would otherwise pass a test that
/// re-implements the strategy correctly while the running app does not. There are no features yet to
/// exercise through HTTP (see .scratch/tutorial-project-common-stack/issues/02-project-and-test-harness-scaffolding.md),
/// so this reads and writes directly against <see cref="TodoTasksDbContext"/> - the smallest thing that
/// still proves the risk - instead of through an endpoint.
/// </remarks>
public sealed class InMemorySqliteConnectionPersistenceTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public InMemorySqliteConnectionPersistenceTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task DataWrittenInOneScope_IsReadableInAnotherScope()
    {
        // Arrange - two independent DI scopes, exactly like two separate HTTP requests would each get.
        using var writeScope = this.factory.Services.CreateScope();
        using var readScope = this.factory.Services.CreateScope();

        var writeContext = writeScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();
        var readContext = readScope.ServiceProvider.GetRequiredService<TodoTasksDbContext>();

        // Two different scopes must resolve two different scoped DbContext instances - otherwise this
        // test would only be proving that reads and writes agree within one context's own change
        // tracker, not that the underlying database itself is shared.
        Assert.NotSame(writeContext, readContext);

        // TodoTasksDbContext has no entities yet (see its own remarks) - a raw table is created and
        // queried directly, the same way a feature ticket without a mapped entity would still be able
        // to prove out its own persistence, per documentation/transactions-and-store-integration.md's
        // "Whole-session protection" section on dropping to the DbContext directly.
        await writeContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE ScaffoldingSmokeTest (Value TEXT NOT NULL)",
            TestContext.Current.CancellationToken
        );

        // Act
        await writeContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO ScaffoldingSmokeTest (Value) VALUES ({0})",
            ["written-in-one-scope"],
            TestContext.Current.CancellationToken
        );

        var valuesReadFromTheOtherScope = await readContext
            .Database.SqlQueryRaw<string>("SELECT Value FROM ScaffoldingSmokeTest")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["written-in-one-scope"], valuesReadFromTheOtherScope);
    }
}
