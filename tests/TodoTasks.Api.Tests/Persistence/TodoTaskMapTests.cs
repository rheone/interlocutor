namespace TodoTasks.Api.Tests.Persistence;

using TodoTasks.Api.Domain;
using TodoTasks.Api.Persistence;

/// <summary>
/// Proves <see cref="TodoTaskMap"/> actually round-trips a <see cref="TodoTask"/> through a real
/// in-memory SQLite database - the mapping-mistake safety net <c>Unit/Features/Tasks/*</c>'s
/// NSubstitute-mocked handler tests can't provide.
/// </summary>
public sealed class TodoTaskMapTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public TodoTaskMapTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public void SaveThenGet_ATodoTask_RoundTripsEveryMappedMember_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", "Whole milk, 2%");

        // Act
        using (var writeSession = this.fixture.OpenSession())
        {
            writeSession.Save(task);
            writeSession.Flush();
        }

        TodoTask? reloaded;
        using (var readSession = this.fixture.OpenSession())
        {
            reloaded = readSession.Get<TodoTask>(task.Id);
        }

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(task.Id, reloaded.Id);
        Assert.Equal("Buy milk", reloaded.Title);
        Assert.Equal("owner-1", reloaded.OwnerId);
        Assert.Equal("Whole milk, 2%", reloaded.Notes);
        Assert.False(reloaded.IsCompleted);
        Assert.Equal(task.CreatedAtUtc, reloaded.CreatedAtUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SaveThenGet_ATodoTaskWithNoNotes_RoundTripsWithNullNotes_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", notes: null);

        // Act
        using (var writeSession = this.fixture.OpenSession())
        {
            writeSession.Save(task);
            writeSession.Flush();
        }

        TodoTask? reloaded;
        using (var readSession = this.fixture.OpenSession())
        {
            reloaded = readSession.Get<TodoTask>(task.Id);
        }

        // Assert
        Assert.NotNull(reloaded);
        Assert.Null(reloaded.Notes);
    }
}
