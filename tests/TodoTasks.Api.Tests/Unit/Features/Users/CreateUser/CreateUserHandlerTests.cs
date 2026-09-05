namespace TodoTasks.Api.Tests.Unit.Features.Users.CreateUser;

using System.Linq.Expressions;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;

/// <summary>
/// Unit tests for <see cref="CreateUserHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IUnitOfWork"/> both NSubstitute-mocked. No
/// database involved; see <c>Persistence/UserPersistenceTests</c> for the real-mapping round-trip and
/// <c>Http/UsersEndpointTests</c> for the full HTTP path.
/// </summary>
public sealed class CreateUserHandlerTests
{
    private readonly IDomainRepository<User, int> users = Substitute.For<IDomainRepository<User, int>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateUserHandler handler;

    public CreateUserHandlerTests() => this.handler = new CreateUserHandler(this.users, this.unitOfWork);

    [Fact]
    public async Task Handle_NoExistingUserWithThatEmail_InsertsAndReturnsTheNewUser_Test()
    {
        // Arrange
        this.users.GetManyAsync(Arg.Any<Expression<Func<User, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);
        var command = new CreateUserCommand("Ada Lovelace", "ada@example.com");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<UserDto>(result.Value);
        Assert.Equal("Ada Lovelace", dto.DisplayName);
        Assert.Equal("ada@example.com", dto.Email);
        await this
            .users.Received(1)
            .InsertAsync(
                Arg.Is<User>(u => u.DisplayName == "Ada Lovelace" && u.Email == "ada@example.com"),
                Arg.Any<CancellationToken>()
            );
        await this.unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnotherUserAlreadyHasThatEmail_ReturnsConflict_WithoutInsertingOrSaving_Test()
    {
        // Arrange
        var existing = new User("Existing User", "taken@example.com");
        this.users.GetManyAsync(Arg.Any<Expression<Func<User, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns([existing]);
        var command = new CreateUserCommand("New User", "taken@example.com");

        // Act
        var result = await this.handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var conflict = Assert.IsType<Conflict>(result.Value);
        Assert.Equal("duplicate_email", conflict.Code);
        await this.users.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await this.unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
