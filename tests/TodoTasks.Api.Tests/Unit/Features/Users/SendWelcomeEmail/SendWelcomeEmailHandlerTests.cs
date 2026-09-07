namespace TodoTasks.Api.Tests.Unit.Features.Users.SendWelcomeEmail;

using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.SendWelcomeEmail;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Unit tests for <see cref="SendWelcomeEmailHandler"/> - the handler logic in isolation, with
/// <see cref="IDomainRepository{TEntity,TKey}"/> and <see cref="IWelcomeMailerClient"/> both
/// NSubstitute-mocked. Proves both that the handler adapts <see cref="WelcomeMailerSendResult"/> into
/// <see cref="SendWelcomeEmailResult"/> correctly, and that the resulting response's active case is
/// readable back out via its public <see cref="SendWelcomeEmailResult.CaseValue"/> - the same shape a
/// caller going through <c>System.Runtime.CompilerServices.IUnion.Value</c> would see.
/// </summary>
public sealed class SendWelcomeEmailHandlerTests
{
    private readonly IDomainRepository<User, int> users = Substitute.For<IDomainRepository<User, int>>();
    private readonly IWelcomeMailerClient mailer = Substitute.For<IWelcomeMailerClient>();
    private readonly SendWelcomeEmailHandler handler;

    public SendWelcomeEmailHandlerTests() => this.handler = new SendWelcomeEmailHandler(this.users, this.mailer);

    [Fact]
    public async Task HandleAsync_AnExistingUser_TheMailerAccepts_ReturnsSuccess_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com") { Id = 42 };
        this.users.GetById(42).Returns(user);
        this.mailer.SendAsync("ada@example.com", "Ada Lovelace", Arg.Any<CancellationToken>())
            .Returns(new WelcomeMailerSendResult(Accepted: true, MessageId: "provider-msg-1"));

        // Act
        var result = await this.handler.HandleAsync(new SendWelcomeEmailCommand(42), CancellationToken.None);

        // Assert
        Assert.IsType<Success>(result.CaseValue);
    }

    [Fact]
    public async Task HandleAsync_AnExistingUser_TheMailerRejectsTheAddress_ReturnsFailureCarryingTheReason_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.invalid") { Id = 42 };
        this.users.GetById(42).Returns(user);
        this.mailer.SendAsync("ada@example.invalid", "Ada Lovelace", Arg.Any<CancellationToken>())
            .Returns(new WelcomeMailerSendResult(Accepted: false, RejectionReason: "not deliverable"));

        // Act
        var result = await this.handler.HandleAsync(new SendWelcomeEmailCommand(42), CancellationToken.None);

        // Assert
        var failure = Assert.IsType<Failure>(result.CaseValue);
        Assert.Equal("not deliverable", failure.Reason);
    }

    [Fact]
    public async Task HandleAsync_NoUserWithThatId_ReturnsNotFound_WithoutCallingTheMailer_Test()
    {
        // Arrange
        this.users.GetById(42).Returns((User?)null);

        // Act
        var result = await this.handler.HandleAsync(new SendWelcomeEmailCommand(42), CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.CaseValue);
        await this.mailer.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
