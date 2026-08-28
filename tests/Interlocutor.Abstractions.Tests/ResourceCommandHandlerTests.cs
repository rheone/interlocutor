namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402

public record TestCommand(int EntityId) : ICommand<OperationOutcome<string, NotFound, Failure>>;

public class TestEntity
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class TestResourceCommandHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
{
    private readonly TestEntity? _loadedEntity;
    private readonly Failure? _authorizationFailure;
    private readonly OperationOutcome<string, NotFound, Failure> _handleResult;

    public TestResourceCommandHandler(
        TestEntity? loadedEntity,
        Failure? authorizationFailure,
        OperationOutcome<string, NotFound, Failure> handleResult
    )
    {
        this._loadedEntity = loadedEntity;
        this._authorizationFailure = authorizationFailure;
        this._handleResult = handleResult;
    }

    protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
        Task.FromResult(this._loadedEntity);

    protected override Failure? Authorize(TestEntity entity, TestCommand command) => this._authorizationFailure;

    protected override Task<OperationOutcome<string, NotFound, Failure>> HandleAsync(
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
        var expectedResult = new OperationOutcome<string, NotFound, Failure>("Success");
        var handler = new TestResourceCommandHandler(entity, null, expectedResult);

        // Act
        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Success", (string)result.Value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        // Arrange
        var handler = new TestResourceCommandHandler(null, null, new OperationOutcome<string, NotFound, Failure>("unused"));

        // Act
        var result = await handler.Handle(new TestCommand(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("TestEntity", notFound.Message);
        Assert.Contains("not found", notFound.Message);
    }

    [Fact]
    public async Task Handle_WithAuthorizationFailure_ReturnsFailure()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var authFailure = new Failure("Access denied");
        var handler = new TestResourceCommandHandler(
            entity,
            authFailure,
            new OperationOutcome<string, NotFound, Failure>("unused")
        );

        // Act
        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        var failure = Assert.IsType<Failure>(result.Value);
        Assert.Equal("Access denied", failure.Reason);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task Handle_WithMissingEntity_AndOverriddenDescribeKey_UsesOverrideInNotFoundMessage()
    {
        // Arrange - DescribeKey's default implementation returns the command itself; a subclass overriding
        // it should have that override reflected in the NotFound message instead of the raw command.
        var handler = new DescribeKeyOverridingHandler();

        // Act
        var result = await handler.Handle(new TestCommand(42), CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("entity-42", notFound.Message);
    }

    private sealed class DescribeKeyOverridingHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
    {
        protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
            Task.FromResult<TestEntity?>(null);

        protected override Failure? Authorize(TestEntity entity, TestCommand command) => null;

        protected override Task<OperationOutcome<string, NotFound, Failure>> HandleAsync(
            TestCommand command,
            TestEntity entity,
            CancellationToken ct
        ) => throw new InvalidOperationException("Should not be called when the entity is missing.");

        protected override object DescribeKey(TestCommand command) => $"entity-{command.EntityId}";
    }
}
