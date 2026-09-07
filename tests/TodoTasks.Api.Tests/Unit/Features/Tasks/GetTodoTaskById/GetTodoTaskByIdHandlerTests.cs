namespace TodoTasks.Api.Tests.Unit.Features.Tasks.GetTodoTaskById;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.GetTodoTaskById;

/// <summary>
/// Unit tests for <see cref="GetTodoTaskByIdHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> NSubstitute-mocked. No database involved; see
/// <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping round-trip and
/// <c>Http/TodoTasksEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class GetTodoTaskByIdHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly GetTodoTaskByIdHandler handler;

    public GetTodoTaskByIdHandlerTests() => this.handler = new GetTodoTaskByIdHandler(this.tasks);

    [Fact]
    public async Task Handle_TaskExistsAndIsOwnedByTheCaller_ReturnsItsDto_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", "2%, please") { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        var query = new GetTodoTaskByIdQuery(42, "owner-1");

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy milk", dto.Title);
        Assert.Equal("2%, please", dto.Notes);
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(42).Returns((TodoTask?)null);
        var query = new GetTodoTaskByIdQuery(42, "owner-1");

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task Handle_TaskExistsButIsOwnedBySomeoneElse_ReturnsNotFound_Test()
    {
        // Arrange - the same NotFound as "doesn't exist at all" (see the handler's own remarks): a
        // non-owner must never be able to tell "no such task" apart from "not yours" via the response.
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        var query = new GetTodoTaskByIdQuery(42, "owner-2");

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
