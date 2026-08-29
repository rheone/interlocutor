namespace TodoTasks.Api.Tests.Unit.Features.Tasks.ClearCompletedTodoTasks;

using System.Linq.Expressions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

/// <summary>Unit tests for <see cref="ClearCompletedTodoTasksHandler"/> - the bare-<c>ICommand</c>/<see cref="IVoidCommandHandler{TCommand,TResult}"/> example.</summary>
public sealed class ClearCompletedTodoTasksHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ClearCompletedTodoTasksHandler handler;

    public ClearCompletedTodoTasksHandlerTests() =>
        this.handler = new ClearCompletedTodoTasksHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_TheOwnerHasCompletedTasks_DeletesOnlyThoseAndSavesOnce_Test()
    {
        // Arrange
        var completed1 = new TodoTask("Buy milk", "owner-1", null) { IsCompleted = true };
        var completed2 = new TodoTask("Walk the dog", "owner-1", null) { IsCompleted = true };
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([completed1, completed2]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await this.tasks.Received(1).DeleteAsync(completed1, Arg.Any<CancellationToken>());
        await this.tasks.Received(1).DeleteAsync(completed2, Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TheOwnerHasNoCompletedTasks_SucceedsWithoutDeletingAnything_Test()
    {
        // Arrange
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        var result = await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await this.tasks.DidNotReceive().DeleteAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesAFilterScopedToBothTheOwnerAndCompletedTasks_Test()
    {
        // Arrange
        Expression<Func<TodoTask, bool>>? capturedFilter = null;
        this.tasks.GetManyAsync(
                Arg.Do<Expression<Func<TodoTask, bool>>?>(f => capturedFilter = f),
                cancellationToken: Arg.Any<CancellationToken>()
            )
            .Returns([]);
        var command = new ClearCompletedTodoTasksCommand("owner-1");

        // Act
        await this.handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedFilter);
        var compiled = capturedFilter.Compile();
        Assert.True(compiled(new TodoTask("Buy milk", "owner-1", null) { IsCompleted = true }));
        Assert.False(compiled(new TodoTask("Buy milk", "owner-1", null) { IsCompleted = false })); // Wrong completion state.
        Assert.False(compiled(new TodoTask("Buy milk", "owner-2", null) { IsCompleted = true })); // Wrong owner.
    }
}
