namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402

public record TestCommand(int EntityId) : ICommand<Outcome<string, NotFound, Forbidden>>;

public class TestEntity
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class TestResourceCommandHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
{
    private readonly TestEntity? _loadedEntity;
    private readonly Forbidden? _authorizationDenial;
    private readonly Outcome<string, NotFound, Forbidden> _handleResult;

    public TestResourceCommandHandler(
        TestEntity? loadedEntity,
        Forbidden? authorizationDenial,
        Outcome<string, NotFound, Forbidden> handleResult
    )
    {
        this._loadedEntity = loadedEntity;
        this._authorizationDenial = authorizationDenial;
        this._handleResult = handleResult;
    }

    protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
        Task.FromResult(this._loadedEntity);

    protected override Forbidden? Authorize(TestEntity entity, TestCommand command) => this._authorizationDenial;

    protected override Task<Outcome<string, NotFound, Forbidden>> HandleAsync(
        TestCommand command,
        TestEntity entity,
        CancellationToken ct
    ) => Task.FromResult(this._handleResult);
}

public class ResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingEntity_ReturnsHandlerResult()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var expectedResult = new Outcome<string, NotFound, Forbidden>("Success");
        var handler = new TestResourceCommandHandler(entity, null, expectedResult);

        // Act
        var result = await handler.HandleAsync(new TestCommand(1), CancellationToken.None);

        // Assert
        Assert.True(result.TryGetValue(out string _));
        Assert.Equal("Success", (string)result.Value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        // Arrange
        var handler = new TestResourceCommandHandler(null, null, new Outcome<string, NotFound, Forbidden>("unused"));

        // Act
        var result = await handler.HandleAsync(new TestCommand(999), CancellationToken.None);

        // Assert
        Assert.False(result.TryGetValue(out string _));
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("TestEntity", notFound.Message);
        Assert.Contains("not found", notFound.Message);
    }

    [Fact]
    public async Task Handle_WithAuthorizationDenial_ReturnsForbidden()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var denial = new Forbidden("Access denied");
        var handler = new TestResourceCommandHandler(entity, denial, new Outcome<string, NotFound, Forbidden>("unused"));

        // Act
        var result = await handler.HandleAsync(new TestCommand(1), CancellationToken.None);

        // Assert
        Assert.False(result.TryGetValue(out string _));
        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Equal("Access denied", forbidden.Reason);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task Handle_WithMissingEntity_AndOverriddenDescribeKey_UsesOverrideInNotFoundMessage()
    {
        // Arrange - DescribeKey's default implementation returns the command itself; a subclass overriding
        // it should have that override reflected in the NotFound message instead of the raw command.
        var handler = new DescribeKeyOverridingHandler();

        // Act
        var result = await handler.HandleAsync(new TestCommand(42), CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("entity-42", notFound.Message);
    }

    private sealed class DescribeKeyOverridingHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
    {
        protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
            Task.FromResult<TestEntity?>(null);

        protected override Forbidden? Authorize(TestEntity entity, TestCommand command) => null;

        protected override Task<Outcome<string, NotFound, Forbidden>> HandleAsync(
            TestCommand command,
            TestEntity entity,
            CancellationToken ct
        ) => throw new InvalidOperationException("Should not be called when the entity is missing.");

        protected override object DescribeKey(TestCommand command) => $"entity-{command.EntityId}";
    }
}
