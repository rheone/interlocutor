namespace TodoTasks.Api.Tests.Unit.Features.Users.DeleteUser;

using System.Linq.Expressions;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.DeleteUser;

/// <summary>
/// Unit tests for <see cref="DeleteUserHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked. No
/// database involved; see <c>Persistence/DeleteUserCascadeTests</c> for the real-database proof that the
/// cascade actually persists, and that a mid-flight failure actually rolls everything back.
/// </summary>
public sealed class DeleteUserHandlerTests
{
    private readonly IDomainRepository<User, int> users = Substitute.For<IDomainRepository<User, int>>();
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteUserHandler handler;

    public DeleteUserHandlerTests() => this.handler = new DeleteUserHandler(this.users, this.tasks, this.unitOfWork);

    [Fact]
    public async Task Handle_AUserWithTasks_DeletesEveryOwnedTaskAndTheUser_ViaOneSaveChangesCall_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com") { Id = 42 };
        var task1 = new TodoTask("Buy milk", "42", null);
        var task2 = new TodoTask("Walk the dog", "42", null);
        this.users.GetById(42).Returns(user);
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([task1, task2]);

        // Act
        var result = await this.handler.Handle(new DeleteUserCommand(42), CancellationToken.None);

        // Assert
        Assert.IsType<Success>(result.Value);
        await this.tasks.Received(1).DeleteAsync(task1, Arg.Any<CancellationToken>());
        await this.tasks.Received(1).DeleteAsync(task2, Arg.Any<CancellationToken>());
        await this.users.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
        // One commit for all of it - the actual point of "one unit of work, many repositories."
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AUserWithNoTasks_StillDeletesTheUserSuccessfully_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com") { Id = 42 };
        this.users.GetById(42).Returns(user);
        this.tasks.GetManyAsync(Arg.Any<Expression<Func<TodoTask, bool>>?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await this.handler.Handle(new DeleteUserCommand(42), CancellationToken.None);

        // Assert
        Assert.IsType<Success>(result.Value);
        await this.tasks.DidNotReceive().DeleteAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.users.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoUserWithThatId_ReturnsNotFound_AndTouchesNeitherRepository_Test()
    {
        // Arrange
        this.users.GetById(42).Returns((User?)null);

        // Act
        var result = await this.handler.Handle(new DeleteUserCommand(42), CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
        await this.tasks.DidNotReceive().GetManyAsync(cancellationToken: Arg.Any<CancellationToken>());
        await this.tasks.DidNotReceive().DeleteAsync(Arg.Any<TodoTask>(), Arg.Any<CancellationToken>());
        await this.users.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
