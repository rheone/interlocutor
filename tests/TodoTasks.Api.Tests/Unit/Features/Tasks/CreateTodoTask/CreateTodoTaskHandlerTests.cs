namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CreateTodoTask;

using System.Linq.Expressions;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;

/// <summary>
/// Unit tests for <see cref="CreateTodoTaskHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked.
/// No database involved; see <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping round-trip
/// and <c>Http/TodoTasksEndpointTests</c> for the full HTTP path.
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
        var command = new CreateTodoTaskCommand("Buy milk", "2%, please", "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy milk", dto.Title);
        Assert.Equal("2%, please", dto.Notes);
        Assert.False(dto.IsCompleted);
        await this
            .tasks.Received(1)
            .InsertAsync(
                Arg.Is<TodoTask>(t => t.Title == "Buy milk" && t.OwnerId == "owner-1" && t.Notes == "2%, please"),
                Arg.Any<CancellationToken>()
            );
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameOwnerAlreadyHasATaskWithThatTitle_ReturnsConflict_WithoutInsertingOrSaving_Test()
    {
        // Arrange
        var existing = new TodoTask("Buy milk", "owner-1", notes: null);
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([existing]);
        var command = new CreateTodoTaskCommand("Buy milk", "Oat milk this time", "owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("duplicate_title", conflict.Code);
        await this.tasks.DidNotReceive().InsertAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ADifferentOwnerHasATaskWithTheSameTitle_StillInsertsForThisOwner_Test()
    {
        // Arrange - the duplicate-title check is scoped per-owner, not global: GetManyAsync is invoked
        // with a filter that already excludes another owner's same-titled task, so an empty result here
        // is what a real repository would return for this owner even though "Buy milk" exists globally.
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);
        var command = new CreateTodoTaskCommand("Buy milk", null, "owner-2");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await this.tasks.Received(1).InsertAsync(Arg.Is<TodoTask>(t => t.OwnerId == "owner-2"), Arg.Any<CancellationToken>());
    }
}
