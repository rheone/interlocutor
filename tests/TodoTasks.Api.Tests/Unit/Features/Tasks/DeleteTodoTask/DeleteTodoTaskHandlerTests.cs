namespace TodoTasks.Api.Tests.Unit.Features.Tasks.DeleteTodoTask;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.DeleteTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Unit tests for <see cref="DeleteTodoTaskHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked.
/// The pipeline's own ownership enforcement (<see cref="TodoTasksPolicyEvaluator"/>, run by
/// <c>AuthorizationBehavior</c> before this handler ever runs) is covered separately in
/// <c>Unit/Infrastructure/TodoTasksPolicyEvaluatorTests</c> and end-to-end in
/// <c>Http/DeleteTodoTaskEndpointTests</c> - this class only exercises what the handler itself still
/// needs to check once authorization has already passed.
/// </summary>
public sealed class DeleteTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteTodoTaskHandler handler;

    public DeleteTodoTaskHandlerTests() => this.handler = new DeleteTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_AnExistingTask_DeletesItAndReturnsSuccess_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        var command = new DeleteTodoTaskCommand(42);

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<Success>(result.Value);
        await this.tasks.Received(1).DeleteAsync(task, Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_ReturnsNotFound_WithoutDeletingOrSaving_Test()
    {
        // Arrange
        this.tasks.GetById(42).Returns((TodoTask?)null);
        var command = new DeleteTodoTaskCommand(42);

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
        await this.tasks.DidNotReceive().DeleteAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
