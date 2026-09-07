namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ReopenTodoTask;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ReopenTodoTask;

/// <summary>
/// Unit tests for <see cref="ReopenTodoTaskHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked.
/// No database involved; see <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping
/// round-trip and <c>Http/CompleteReopenTodoTaskEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class ReopenTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ReopenTodoTaskHandler handler;

    public ReopenTodoTaskHandlerTests() => this.handler = new ReopenTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_ACompletedTaskOwnedByTheCaller_ReopensItAndSaves_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1, IsCompleted = true };
        this.tasks.GetById(1).Returns(task);
        var command = new ReopenTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.False(dto.IsCompleted);
        Assert.False(task.IsCompleted);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ATaskThatIsNotCompleted_ReturnsConflict_WithoutSaving_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1 };
        this.tasks.GetById(1).Returns(task);
        var command = new ReopenTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("not_completed", conflict.Code);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_WithoutMutatingOrSaving_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 1, IsCompleted = true };
        this.tasks.GetById(1).Returns(task);
        var command = new ReopenTodoTaskCommand(1, "owner-2");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<Forbidden>(result.Value);
        Assert.True(task.IsCompleted);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.tasks.GetById(1).Returns((TodoTask?)null);
        var command = new ReopenTodoTaskCommand(1, "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
