namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CreateTodoTask;

using System.Linq.Expressions;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;

/// <summary>
/// Unit tests for <see cref="CreateTodoTaskHandler"/> - mirrors <see cref="TodoTasks.Api.Tests.Unit.Features.Users.CreateUser.CreateUserHandlerTests"/>'s
/// shape exactly, since both handlers follow the same "duplicate check, then insert" pattern.
/// </summary>
public sealed class CreateTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateTodoTaskHandler handler;

    public CreateTodoTaskHandlerTests() => this.handler = new CreateTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_NoExistingTaskWithThatTitleForThisOwner_InsertsAndReturnsTheNewTask_Test()
    {
        // Arrange
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);
        var command = new CreateTodoTaskCommand("Buy milk", "Whole milk, 2%", "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy milk", dto.Title);
        Assert.Equal("Whole milk, 2%", dto.Notes);
        Assert.False(dto.IsCompleted);
        await this
            .tasks.Received(1)
            .InsertAsync(Arg.Is<TodoTask>(t => t.Title == "Buy milk" && t.OwnerId == "owner-1"), Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameOwnerAlreadyHasATaskWithThatTitle_ReturnsConflict_WithoutInsertingOrSaving_Test()
    {
        // Arrange
        var existing = new TodoTask("Buy milk", "owner-1", notes: null);
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([existing]);
        var command = new CreateTodoTaskCommand("Buy milk", null, "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("duplicate_title", conflict.Code);
        await this.tasks.DidNotReceive().InsertAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
