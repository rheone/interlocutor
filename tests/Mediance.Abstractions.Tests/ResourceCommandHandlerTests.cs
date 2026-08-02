namespace Mediance.Abstractions.Tests;

using Mediance.Abstractions.Commands;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

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
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var expectedResult = new OperationOutcome<string, NotFound, Failure>("Success");
        var handler = new TestResourceCommandHandler(entity, null, expectedResult);

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Success", (string)result.Value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        var handler = new TestResourceCommandHandler(null, null, new OperationOutcome<string, NotFound, Failure>("unused"));

        var result = await handler.Handle(new TestCommand(999), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var notFound = Assert.IsType<NotFound>(result.Value);
        Assert.Contains("TestEntity", notFound.Message);
        Assert.Contains("not found", notFound.Message);
    }

    [Fact]
    public async Task Handle_WithAuthorizationFailure_ReturnsFailure()
    {
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var authFailure = new Failure("Access denied");
        var handler = new TestResourceCommandHandler(
            entity,
            authFailure,
            new OperationOutcome<string, NotFound, Failure>("unused")
        );

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var failure = Assert.IsType<Failure>(result.Value);
        Assert.Equal("Access denied", failure.Reason);
    }
}
