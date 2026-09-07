namespace TodoTasks.Api.Tests.Unit.Features.Users.GetUserById;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.GetUserById;

/// <summary>
/// Unit tests for <see cref="GetUserByIdHandler"/> - mirrors
/// <c>documentation/beginners-guide-todo-api.md</c> Step 6's <c>GetTodoTaskByIdHandler</c>'s plain
/// read-only shape exactly.
/// </summary>
public sealed class GetUserByIdHandlerTests
{
    private readonly IDomainRepository<User, int> users = Substitute.For<IDomainRepository<User, int>>();
    private readonly GetUserByIdHandler handler;

    public GetUserByIdHandlerTests() => this.handler = new GetUserByIdHandler(this.users);

    [Fact]
    public async Task Handle_UserExists_ReturnsItsDto_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com");
        this.users.GetById(42).Returns(user);
        var query = new GetUserByIdQuery(42);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = Assert.IsType<UserDto>(result.Value);
        Assert.Equal("Ada Lovelace", dto.DisplayName);
    }

    [Fact]
    public async Task Handle_NoUserWithThatId_ReturnsNotFound_Test()
    {
        // Arrange
        this.users.GetById(42).Returns((User?)null);
        var query = new GetUserByIdQuery(42);

        // Act
        var result = await this.handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }
}
