namespace TodoTasks.Api.Tests.Unit.Features.Tasks.GetTodoTaskById;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.GetTodoTaskById;

/// <summary>Unit tests for <see cref="GetTodoTaskByIdHandler"/> - mirrors <c>GetUserByIdHandler</c>'s plain read shape exactly.</summary>
public sealed class GetTodoTaskByIdHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly GetTodoTaskByIdHandler handler;

    public GetTodoTaskByIdHandlerTests() => this.handler = new GetTodoTaskByIdHandler(this.tasks);

    [Fact]
    public async Task Handle_TaskExists_ReturnsItsDto_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(42).Returns(task);
        var query = new GetTodoTaskByIdQuery(42);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy milk", dto.Title);
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(42).Returns((TodoTask?)null);
        var query = new GetTodoTaskByIdQuery(42);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
