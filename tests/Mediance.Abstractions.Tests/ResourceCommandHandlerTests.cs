using Mediance.Abstractions.Commands;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

namespace Mediance.Abstractions.Tests;

public record TestCommand(int entityId) : ICommand<Result<string, NotFound, Failure>>;

public class TestEntity
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class ResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingEntity_ReturnsHandlerResult()
    {
        var handler = new StubHandler
        {
            LoadResult = new TestEntity { Id = 1, Name = "Test" },
            HandleResult = "Processed",
        };

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        var value = result switch
        {
            string s => s,
            NotFound => null,
            Failure => null,
        };
        Assert.Equal("Processed", value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        var handler = new StubHandler { LoadResult = null };

        var result = await handler.Handle(new TestCommand(99), CancellationToken.None);

        var isNotFound = result switch
        {
            NotFound => true,
            string => false,
            Failure => false,
        };
        Assert.True(isNotFound);
    }

    [Fact]
    public async Task Handle_WithAuthorizationFailure_ReturnsFailure()
    {
        var handler = new StubHandler
        {
            LoadResult = new TestEntity { Id = 1 },
            AuthorizeResult = new Failure("Access denied"),
        };

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        var failure = result switch
        {
            Failure f => f,
            string => null,
            NotFound => null,
        };
        Assert.NotNull(failure);
        Assert.Equal("Access denied", failure!.Reason);
    }
}

public class StubHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
{
    public TestEntity? LoadResult { get; set; }

    public Failure? AuthorizeResult { get; set; }

    public Result<string, NotFound, Failure> HandleResult { get; set; }

    protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
        Task.FromResult(this.LoadResult);

    protected override Failure? Authorize(TestEntity entity, TestCommand command) => this.AuthorizeResult;

    protected override Task<Result<string, NotFound, Failure>> HandleAsync(
        TestCommand command,
        TestEntity entity,
        CancellationToken ct
    ) => Task.FromResult(this.HandleResult);
}
