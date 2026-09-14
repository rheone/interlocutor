namespace TodoTasks.Api.Tests.Unit.Features.Tasks.UpdateTodoTask;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;

/// <summary>
/// Unit tests for <see cref="UpdateTodoTaskHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked.
/// No database involved; see <c>Persistence/TodoTaskPersistenceTests</c> for the real-mapping round-trip
/// and <c>Http/TodoTasksEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class UpdateTodoTaskHandlerTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateTodoTaskHandler handler;

    public UpdateTodoTaskHandlerTests() => this.handler = new UpdateTodoTaskHandler(this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_TaskExistsAndIsOwnedByTheCaller_UpdatesItAndSaves_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", "2%, please") { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        var command = new UpdateTodoTaskCommand(42, "Buy oat milk", "Oat milk this time", "owner-1");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<TodoTaskDto>(result.Value);
        Assert.Equal("Buy oat milk", dto.Title);
        Assert.Equal("Oat milk this time", dto.Notes);
        Assert.Equal("Buy oat milk", task.Title);
        Assert.Equal("Oat milk this time", task.Notes);
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTaskWithThatId_EjectsNotFound_WithoutSaving_Test()
    {
        // Arrange - ResourceCommandHandler{TCommand,TEntity,TResponse} ejects NotFound via Eject.WithCase
        // rather than constructing it directly; only the mediator's own dispatch machinery converts that
        // eject back into an ordinary response (see ResourceCommandHandler's own remarks), so a call directly
        // against the handler - as this "in isolation" test does - observes the eject signal itself instead.
        this.tasks.GetById(42).Returns((TodoTask?)null);
        var command = new UpdateTodoTaskCommand(42, "Buy oat milk", null, "owner-1");

        // Act
        var ex = await Assert.ThrowsAsync<PipelineEjectCaseException>(() =>
            this.handler.HandleAsync(command, CancellationToken.None)
        );

        // Assert
        Assert.IsType<NotFound>(ex.CaseValue);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TaskExistsButIsOwnedBySomeoneElse_EjectsForbidden_WithoutMutatingOrSaving_Test()
    {
        // Arrange - unlike GetTodoTaskByIdHandler (which folds "not yours" into the same NotFound as "no
        // such task" to avoid leaking existence to a non-owner on a plain read), ResourceCommandHandler's
        // load/authorize/handle template has already settled the existence question by the time Authorize
        // runs - see UpdateTodoTaskHandler's own remarks for why Forbidden is the right case here. See the
        // NotFound test above for why this "in isolation" call observes the eject signal itself.
        var task = new TodoTask("Buy milk", "owner-1", "2%, please") { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        var command = new UpdateTodoTaskCommand(42, "Hijacked title", "Hijacked notes", "owner-2");

        // Act
        var ex = await Assert.ThrowsAsync<PipelineEjectCaseException>(() =>
            this.handler.HandleAsync(command, CancellationToken.None)
        );

        // Assert
        Assert.IsType<Forbidden>(ex.CaseValue);
        Assert.Equal("Buy milk", task.Title);
        Assert.Equal("2%, please", task.Notes);
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
