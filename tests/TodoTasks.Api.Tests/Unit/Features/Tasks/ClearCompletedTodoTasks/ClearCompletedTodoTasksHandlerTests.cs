namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ClearCompletedTodoTasks;

using System.Linq.Expressions;
using Interlocutor.CommonKernel;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

/// <summary>
/// Unit tests for <see cref="ClearCompletedTodoTasksHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked.
/// No database involved; see <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping bulk
/// delete proven against a real database.
/// </summary>
public sealed class ClearCompletedTodoTasksHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ClearCompletedTodoTasksHandler handler;

    public ClearCompletedTodoTasksHandlerTests() =>
        this.handler = new ClearCompletedTodoTasksHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_TheOwnerHasCompletedTasks_DeletesEachOneAndSavesAndReturnsSuccess_Test()
    {
        // Arrange - the handler mocked here can't prove the *filter expression itself* excludes an
        // incomplete task or another owner's tasks (that's IDomainRepository's own contract, proven for
        // real against a real database in Persistence/TodoTaskPersistenceTests); what this test proves is
        // that the handler passes OwnerId/IsCompleted through to GetManyAsync's filter, deletes every
        // task the repository hands back, and saves exactly once regardless of how many there were.
        var first = new TodoTask("Buy milk", "owner-1", null) { Id = 1, IsCompleted = true };
        var second = new TodoTask("Wash car", "owner-1", null) { Id = 2, IsCompleted = true };
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([first, second]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert - Outcome is case-less; reaching this line without throwing already is the acknowledgment
        // (see Outcome's own remarks). What's worth proving is the delete/save behavior below.
        Assert.Equal(default(Outcome), result);
        await this.tasks.Received(1).DeleteAsync(first, Arg.Any<CancellationToken>());
        await this.tasks.Received(1).DeleteAsync(second, Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await this
            .tasks.Received(1)
            .GetManyAsync(
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => FilterMatches(f, first)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_TheFilterPassedToTheRepository_ExcludesAnIncompleteTaskForTheSameOwner_Test()
    {
        // Arrange - proves the filter compiled from the command excludes an incomplete task, whereas it
        // would include a completed one for the same owner.
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var incomplete = new TodoTask("Buy milk", "owner-1", null);
        await this
            .tasks.Received(1)
            .GetManyAsync(
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => !FilterMatches(f, incomplete)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_TheFilterPassedToTheRepository_ExcludesACompletedTaskForAnotherOwner_Test()
    {
        // Arrange - proves the filter compiled from the command excludes another owner's completed task.
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var otherOwnersTask = new TodoTask("Buy milk", "owner-2", null) { IsCompleted = true };
        await this
            .tasks.Received(1)
            .GetManyAsync(
                Arg.Is<Expression<Func<TodoTask, bool>>>(f => !FilterMatches(f, otherOwnersTask)),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_NoCompletedTasksForTheOwner_ReturnsSuccess_WithoutDeletingAnything_Test()
    {
        // Arrange
        this.tasks.GetManyAsync(
                Arg.Any<Expression<Func<TodoTask, bool>>>(),
                Arg.Any<IReadOnlyList<SortCriterion<TodoTask>>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert - still saves (a no-op SaveChangesAsync is harmless) and acknowledges even when there
        // was nothing to clear - Outcome is case-less, so there's no distinct "nothing to do" case to report.
        Assert.Equal(default(Outcome), result);
        await this.tasks.DidNotReceive().DeleteAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static bool FilterMatches(Expression<Func<TodoTask, bool>> filter, TodoTask task) => filter.Compile()(task);
}
