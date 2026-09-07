namespace Interlocutor.Testing.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Covers <see cref="FakeSender"/>'s stub-a-response, capture-what-was-sent, and each <c>SendAsync</c> overload.</summary>
public class FakeSenderTests
{
    [Fact]
    public async Task SendAsync_Query_ReturnsStubbedResponse()
    {
        // Arrange
        var sender = new FakeSender();
        sender.SetResponse<GetUser, Outcome<UserDto, NotFound>>(new UserDto(1, "Alice"));
        var query = new GetUser(1);

        // Act
        var result = await sender.SendAsync<Outcome<UserDto, NotFound>>(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.TryGetValue(out UserDto user));
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task SendAsync_ValueReturningCommand_ReturnsStubbedResponse()
    {
        // Arrange
        var sender = new FakeSender();
        sender.SetResponse<CreateUser, Outcome<UserDto, NotFound>>(new UserDto(2, "Bob"));
        var command = new CreateUser("Bob");

        // Act
        var result = await sender.SendAsync<Outcome<UserDto, NotFound>>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.TryGetValue(out UserDto user));
        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public async Task SendAsync_BareCommand_ReturnsStubbedResponseForTheCallSiteChosenResultType()
    {
        // Arrange: the same DeleteUser command, but the caller picks the outcome shape at the call site,
        // exactly as the real ISender.SendAsync<TResult>(ICommand, ...) overload allows.
        var sender = new FakeSender();
        sender.SetResponse<DeleteUser, Outcome<Success, NotFound>>(default(Success));
        var command = new DeleteUser(1);

        // Act
        var result = await sender.SendAsync<Outcome<Success, NotFound>>(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.TryGetValue(out Success _));
    }

    [Fact]
    public async Task SendAsync_RuntimeTypedRequest_ReturnsStubbedResponseAsIUnion()
    {
        // Arrange
        var sender = new FakeSender();
        sender.SetResponse<GetUser, Outcome<UserDto, NotFound>>(new UserDto(1, "Alice"));
        object query = new GetUser(1);

        // Act
        var result = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert: the runtime-typed overload returns the base IUnion interface - its Value accessor still
        // exposes the same case value the type-safe overload's TryGetValue would.
        var user = Assert.IsType<UserDto>(result.Value);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task SendAsync_RecordsEveryRequestInDispatchOrder()
    {
        // Arrange
        var sender = new FakeSender();
        sender.SetResponse<GetUser, Outcome<UserDto, NotFound>>(new UserDto(1, "Alice"));
        sender.SetResponse<CreateUser, Outcome<UserDto, NotFound>>(new UserDto(2, "Bob"));
        var firstQuery = new GetUser(1);
        var secondCommand = new CreateUser("Bob");

        // Act
        await sender.SendAsync<Outcome<UserDto, NotFound>>(firstQuery, TestContext.Current.CancellationToken);
        await sender.SendAsync<Outcome<UserDto, NotFound>>(secondCommand, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([firstQuery, secondCommand], sender.ReceivedRequests);
    }

    [Fact]
    public async Task SendAsync_NoStubbedResponseForRequestType_ThrowsWithTheRequestTypeName()
    {
        // Arrange
        var sender = new FakeSender();
        var query = new GetUser(1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync<Outcome<UserDto, NotFound>>(query, TestContext.Current.CancellationToken)
        );
        Assert.Contains(nameof(GetUser), ex.Message, StringComparison.Ordinal);

        // The unanswered request is still recorded - a test can assert what was sent even when the
        // assertion is that no response was ever configured for it.
        Assert.Same(query, Assert.Single(sender.ReceivedRequests));
    }

    [Fact]
    public void SetResponse_ReturnsTheSameFakeSenderForChaining()
    {
        // Arrange
        var sender = new FakeSender();

        // Act
        var chained = sender.SetResponse<GetUser, Outcome<UserDto, NotFound>>(new UserDto(1, "Alice"));

        // Assert
        Assert.Same(sender, chained);
    }

    private sealed record GetUser(int UserId) : IQuery<Outcome<UserDto, NotFound>>;

    private sealed record CreateUser(string Name) : ICommand<Outcome<UserDto, NotFound>>;

    private sealed record DeleteUser(int UserId) : ICommand;

    private sealed record UserDto(int Id, string Name);
}
