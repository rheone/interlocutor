using System.Diagnostics;
using Mediance.CommonKernel;

namespace Mediance.Abstractions.Tests;

public record TestCommand(int EntityId) : ICommand<string>;

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
            HandleResult = new Result<string>("Processed"),
        };

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        var value = result.Value switch
        {
            string s => s,
            Failure => null,
            _ => throw new UnreachableException(),
        };
        Assert.Equal("Processed", value);
    }

    [Fact]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        var handler = new StubHandler { LoadResult = null };

        var result = await handler.Handle(new TestCommand(99), CancellationToken.None);

        var isFailure = result.Value switch
        {
            Failure => true,
            string => false,
            _ => throw new UnreachableException(),
        };
        Assert.True(isFailure);
    }

    [Fact]
    public async Task Handle_WithAuthorizationFailure_ReturnsForbidden()
    {
        var handler = new StubHandler
        {
            LoadResult = new TestEntity { Id = 1 },
            AuthorizeResult = new Forbidden("Access denied"),
        };

        var result = await handler.Handle(new TestCommand(1), CancellationToken.None);

        var forbidden = result.Value switch
        {
            Failure f => f switch
            {
                Forbidden fb => fb,
                _ => null,
            },
            string => null,
            _ => throw new UnreachableException(),
        };
        Assert.NotNull(forbidden);
        Assert.Equal("Access denied", forbidden!.Reason);
    }
}

public class StubHandler : ResourceCommandHandler<TestCommand, TestEntity, string>
{
    public TestEntity? LoadResult { get; set; }

    public Failure? AuthorizeResult { get; set; }

    public Result<string> HandleResult { get; set; }

    protected override Task<TestEntity?> LoadAsync(TestCommand command, CancellationToken ct) =>
        Task.FromResult(this.LoadResult);

    protected override Failure? Authorize(TestEntity entity, TestCommand command) => this.AuthorizeResult;

    protected override Task<Result<string>> HandleAsync(TestCommand command, TestEntity entity, CancellationToken ct) =>
        Task.FromResult(this.HandleResult);
}
