namespace TodoTasks.Api.Tests.Persistence;

using TodoTasks.Api.Domain;
using TodoTasks.Api.Persistence;

/// <summary>
/// Proves <see cref="UserMap"/> actually round-trips a <see cref="User"/> through a real in-memory
/// SQLite database - the kind of mapping mistake (a wrong column name, a missing <c>Not.Nullable()</c>)
/// <c>Unit/Features/Users/*</c>'s NSubstitute-mocked handler tests can never catch.
/// </summary>
public sealed class UserMapTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public UserMapTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public void SaveThenGet_AUser_RoundTripsEveryMappedMember_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com");

        // Act - save and flush in one session, then read back through a second session opened against
        // the same kept-alive connection, the same way a real dispatch's insert and a later request's
        // read are two different sessions sharing one physical database.
        using (var writeSession = this.fixture.OpenSession())
        {
            writeSession.Save(user);
            writeSession.Flush();
        }

        User? reloaded;
        using (var readSession = this.fixture.OpenSession())
        {
            reloaded = readSession.Get<User>(user.Id);
        }

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(user.Id, reloaded.Id);
        Assert.Equal("Ada Lovelace", reloaded.DisplayName);
        Assert.Equal("ada@example.com", reloaded.Email);
    }
}
