namespace TodoTasks.Api.Tests.Unit.Features.Tasks.UpdateTodoTask;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;

/// <summary>
/// Unit tests for <see cref="UpdateTodoTaskHandler"/>'s load/authorize/handle template
/// (<c>ResourceCommandHandler</c>) - drives it via its public <c>Handle</c> method exactly the way
/// the pipeline does, rather than testing the protected steps in isolation.
/// </summary>
public sealed class UpdateTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateTodoTaskHandler handler;

    public UpdateTodoTaskHandlerTests() => this.handler = new UpdateTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_OwnerUpdatesTheirOwnTask_SavesAndReturnsTheUpdatedTask_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", "old notes");
        this.tasks.GetById(1).Returns(task);
        var command = new UpdateTodoTaskCommand(1, "Buy oat milk", "new notes", "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy oat milk", dto.Title);
        Assert.Equal("new notes", dto.Notes);
        Assert.Equal("Buy oat milk", task.Title); // The same tracked entity was mutated, not a copy.
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TaskDoesNotExist_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new UpdateTodoTaskCommand(1, "Buy oat milk", null, "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallerIsNotTheOwner_ReturnsFailure_WithoutSaving_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(1).Returns(task);
        var command = new UpdateTodoTaskCommand(1, "Buy oat milk", null, "someone-else");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<Failure>(result.Value);
        Assert.Equal("Buy milk", task.Title); // Untouched.
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
