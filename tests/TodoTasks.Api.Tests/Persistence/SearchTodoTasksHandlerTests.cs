namespace TodoTasks.Api.Tests.Persistence;

using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.SearchTodoTasks;

/// <summary>
/// Tests <see cref="SearchTodoTasksHandler"/> against a real in-memory SQLite database, rather than an
/// NSubstitute mock - QueryOver's fluent Criteria API composes onto the concrete <c>ISession</c> it's
/// called on, not through an interface method NSubstitute could meaningfully intercept, so a real session
/// is the only way to prove the resulting SQL actually matches case-insensitively and scopes to the
/// right owner.
/// </summary>
public sealed class SearchTodoTasksHandlerTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public SearchTodoTasksHandlerTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task Handle_ATitleSubstring_MatchesCaseInsensitively_ScopedToTheOwner_Test()
    {
        // Arrange - two tasks for the searching owner (one matching, one not) and a same-titled task
        // belonging to a different owner, which must never appear in the results.
        using var session = this.fixture.OpenSession();
        session.Save(new TodoTask("Buy Milk", "owner-1", null));
        session.Save(new TodoTask("Walk the dog", "owner-1", null));
        session.Save(new TodoTask("Buy Milk", "owner-2", null));
        session.Flush();

        var handler = new SearchTodoTasksHandler(new Lazy<global::NHibernate.ISession>(() => session));
        var query = new SearchTodoTasksQuery("owner-1", "milk"); // Lowercase - proves the match is case-insensitive.

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var matches = Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value);
        var match = Assert.Single(matches);
        Assert.Equal("Buy Milk", match.Title);
    }

    [Fact]
    public async Task Handle_NoTitleContainsTheSubstring_ReturnsAnEmptyList_Test()
    {
        // Arrange - a distinct owner ("owner-3"), not "owner-1"/"owner-2": this fixture's database is
        // shared for the life of the whole test class (see PersistenceFixture's remarks), and the
        // previous test already committed a "Buy Milk" row for owner-1. Reusing owner-1 here would make
        // this test's outcome depend on whether that test happened to run first.
        using var session = this.fixture.OpenSession();
        session.Save(new TodoTask("Walk the dog", "owner-3", null));
        session.Flush();

        var handler = new SearchTodoTasksHandler(new Lazy<global::NHibernate.ISession>(() => session));
        var query = new SearchTodoTasksQuery("owner-3", "milk");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<TodoTaskDto>>(result.Value));
    }
}
