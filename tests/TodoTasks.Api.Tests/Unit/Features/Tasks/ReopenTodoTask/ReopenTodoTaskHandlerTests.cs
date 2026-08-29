namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ReopenTodoTask;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ReopenTodoTask;

/// <summary>Unit tests for <see cref="ReopenTodoTaskHandler"/>.</summary>
public sealed class ReopenTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ReopenTodoTaskHandler handler;

    public ReopenTodoTaskHandlerTests() => this.handler = new ReopenTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_ACompletedTask_ReopensIt_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { IsCompleted = true };
        this.tasks.GetById(1).Returns(task);
        var command = new ReopenTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.False(dto.IsCompleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ATaskThatIsNotCompleted_ReturnsConflict_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(1).Returns(task);
        var command = new ReopenTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("not_completed", conflict.Code);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new ReopenTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
