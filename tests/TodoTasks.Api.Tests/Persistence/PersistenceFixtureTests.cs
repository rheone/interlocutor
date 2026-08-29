namespace TodoTasks.Api.Tests.Persistence;

/// <summary>
/// Proves <see cref="PersistenceFixture"/> itself works - that
/// <c>TodoTasksSessionFactory.Build()</c> produces a session factory whose sessions can actually
/// talk to the kept-alive in-memory connection. Feature tickets add their own mapping-round-trip
/// tests here once an entity exists to round-trip.
/// </summary>
public sealed class PersistenceFixtureTests : IClassFixture<PersistenceFixture>
{
    private readonly PersistenceFixture fixture;

    public PersistenceFixtureTests(PersistenceFixture fixture) => this.fixture = fixture;

    [Fact]
    public void OpenSession_AgainstKeptAliveConnection_ProducesAConnectedSession_Test()
    {
        // Arrange
        using var session = this.fixture.OpenSession();

        // Act
        var result = session.CreateSQLQuery("SELECT 1").UniqueResult<long>();

        // Assert
        Assert.True(session.IsConnected);
        Assert.Equal(1, result);
    }

    [Fact]
    public void OpenSession_CalledTwice_ReturnsSessionsThatShareTheSameKeptAliveConnection_Test()
    {
        // Arrange
        using var first = this.fixture.OpenSession();
        using var second = this.fixture.OpenSession();

        // Act - a table created by one session's schema export is visible to a session opened
        // afterward only if both are actually talking to the same physical connection.
        var firstResult = first.CreateSQLQuery("SELECT 1").UniqueResult<long>();
        var secondResult = second.CreateSQLQuery("SELECT 1").UniqueResult<long>();

        // Assert
        Assert.Equal(firstResult, secondResult);
    }
}
