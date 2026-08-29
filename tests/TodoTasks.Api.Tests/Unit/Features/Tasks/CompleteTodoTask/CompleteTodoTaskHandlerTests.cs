namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;

/// <summary>Unit tests for <see cref="CompleteTodoTaskHandler"/>.</summary>
public sealed class CompleteTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher publisher = Substitute.For<IPublisher>();
    private readonly CompleteTodoTaskHandler handler;

    public CompleteTodoTaskHandlerTests() =>
        this.handler = new CompleteTodoTaskHandler(this.tasks, this.unitOfWork, this.publisher);

    [Fact]
    public async Task Handle_AnIncompleteTask_MarksItComplete_SavesAndPublishes_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(1).Returns(task);
        var command = new CompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.True(dto.IsCompleted);
        Assert.True(task.IsCompleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Persist first, publish second - the guide's own stated ordering. task.Id isn't set here (no
        // real database assigning it, unlike a real dispatch), so this only asserts what a mocked
        // entity can meaningfully prove: the right owner and title were published.
        await this
            .publisher.Received(1)
            .Publish(
                Arg.Is<TodoTaskCompletedNotification>(n => n.OwnerId == "owner-1" && n.Title == "Buy milk"),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_AnAlreadyCompleteTask_ReturnsConflict_WithoutSavingOrPublishing_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { IsCompleted = true };
        this.tasks.GetById(1).Returns(task);
        var command = new CompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("already_completed", conflict.Code);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await this.publisher.DidNotReceive().Publish(Arg.Any<TodoTaskCompletedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new CompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
