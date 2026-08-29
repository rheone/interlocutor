namespace TodoTasks.Api.Tests.Unit.Features.Tasks.SyncAwareCompleteTodoTask;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.SyncAwareCompleteTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>Unit tests for <see cref="SyncAwareCompleteTodoTaskHandler"/>.</summary>
public sealed class SyncAwareCompleteTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IExternalTaskSyncClient externalSync = Substitute.For<IExternalTaskSyncClient>();
    private readonly SyncAwareCompleteTodoTaskHandler handler;

    public SyncAwareCompleteTodoTaskHandlerTests() =>
        this.handler = new SyncAwareCompleteTodoTaskHandler(this.tasks, this.unitOfWork, this.externalSync);

    [Fact]
    public async Task Handle_AlreadyCompletedElsewhere_CancelsInsteadOfSaving_ButStillReturnsSuccess_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(1).Returns(task);
        // Note: a mocked TodoTask never gets a real database-assigned Id (it stays 0), so the sync
        // client's expectation is matched by argument shape, not the specific id.
        this.externalSync.WasAlreadyCompletedElsewhereAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new SyncAwareCompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert - success is reported (the task ends up completed either way), but the write was
        // abandoned via Cancel rather than committed via SaveChangesAsync.
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.True(dto.IsCompleted);
        await this.unitOfWork.Received(1).Cancel(Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotAlreadyCompletedElsewhere_SavesInsteadOfCancelling_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null);
        this.tasks.GetById(1).Returns(task);
        this.externalSync.WasAlreadyCompletedElsewhereAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);
        var command = new SyncAwareCompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.True(dto.IsCompleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().Cancel(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_WithoutCheckingExternalSync_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new SyncAwareCompleteTodoTaskCommand(1);

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
        await this.externalSync.DidNotReceive().WasAlreadyCompletedElsewhereAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
