namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;

/// <summary>
/// Unit tests for <see cref="CompleteTodoTaskHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/>, <see cref="IUnitOfWork"/>, and <see cref="IPublisher"/>
/// all NSubstitute-mocked. No database and no real dispatch involved; see
/// <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping round-trip,
/// <c>Unit/Features/Tasks/CompleteTodoTask/NotificationHandlerTests</c> for the two subscribers in
/// isolation, and <c>Http/CompleteReopenTodoTaskEndpointTests</c> for the full HTTP path including real
/// multi-subscriber fan-out.
/// </summary>
public sealed class CompleteTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher publisher = Substitute.For<IPublisher>();
    private readonly CompleteTodoTaskHandler handler;

    public CompleteTodoTaskHandlerTests() =>
        this.handler = new CompleteTodoTaskHandler(this.tasks, this.unitOfWork, this.publisher);

    [Fact]
    public async Task Handle_AnIncompleteTaskOwnedByTheCaller_MarksItComplete_SavesAndPublishes_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1 };
        this.tasks.GetById(1).Returns(task);
        var command = new CompleteTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.True(dto.IsCompleted);
        Assert.True(task.IsCompleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Persist first, publish second - the guide's own stated ordering.
        await this
            .publisher.Received(1)
            .PublishAsync(
                Arg.Is<TodoTaskCompletedNotification>(n =>
                    n.TodoTaskId == 1 && n.OwnerId == "owner-1" && n.Title == "Buy milk"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_AnAlreadyCompleteTask_ReturnsConflict_WithoutSavingOrPublishing_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1, IsCompleted = true };
        this.tasks.GetById(1).Returns(task);
        var command = new CompleteTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("already_completed", conflict.Code);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await this
            .publisher.DidNotReceive()
            .PublishAsync(Arg.Any<TodoTaskCompletedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_WithoutMutatingSavingOrPublishing_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1 };
        this.tasks.GetById(1).Returns(task);
        var command = new CompleteTodoTaskCommand(1, "owner-2");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        Assert.False(task.IsCompleted);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await this
            .publisher.DidNotReceive()
            .PublishAsync(Arg.Any<TodoTaskCompletedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new CompleteTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
