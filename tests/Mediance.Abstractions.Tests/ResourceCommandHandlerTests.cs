using Mediance.Abstractions.Commands;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

namespace Mediance.Abstractions.Tests;

public record TestCommand(int entityId) : ICommand<OperationOutcome<string, NotFound, Failure>>;

public class TestEntity
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class ResourceCommandHandlerTests
{
    [Fact(Skip = "ResourceCommandHandler has been commented out during code migration")]
    public async Task Handle_WithExistingEntity_ReturnsHandlerResult()
    {
        // ResourceCommandHandler<TCommand, TEntity, TSuccess> is not available in current version
        await Task.CompletedTask;
    }

    [Fact(Skip = "ResourceCommandHandler has been commented out during code migration")]
    public async Task Handle_WithMissingEntity_ReturnsNotFound()
    {
        // ResourceCommandHandler<TCommand, TEntity, TSuccess> is not available in current version
        await Task.CompletedTask;
    }

    [Fact(Skip = "ResourceCommandHandler has been commented out during code migration")]
    public async Task Handle_WithAuthorizationFailure_ReturnsFailure()
    {
        // ResourceCommandHandler<TCommand, TEntity, TSuccess> is not available in current version
        await Task.CompletedTask;
    }
}
