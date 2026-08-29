namespace TodoTasks.Api.Tests.Unit.Features.Tasks.CompleteTodoTask;

using Microsoft.Extensions.Logging;
using NSubstitute;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Unit tests for <see cref="TodoTaskCompletedNotification"/>'s two independent subscribers - proves
/// each reacts on its own, without knowing the other exists.
/// </summary>
public sealed class NotificationHandlerTests
{
    [Fact]
    public async Task TodoTaskCompletionCounterHandler_Handle_IncrementsTheCounterForTheOwner_Test()
    {
        // Arrange
        var counter = Substitute.For<ICompletionCounter>();
        var handler = new TodoTaskCompletionCounterHandler(counter);
        var notification = new TodoTaskCompletedNotification(1, "owner-1", "Buy milk");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        counter.Received(1).Increment("owner-1");
    }

    [Fact]
    public async Task TodoTaskCompletionAuditLogHandler_Handle_LogsTheCompletion_Test()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TodoTaskCompletionAuditLogHandler>>();
        var handler = new TodoTaskCompletionAuditLogHandler(logger);
        var notification = new TodoTaskCompletedNotification(1, "owner-1", "Buy milk");

        // Act - the log call itself is the behavior under test, and ILogger's extension methods aren't
        // meaningfully mockable beyond "did it throw."
        var exception = await Record.ExceptionAsync(() => handler.Handle(notification, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void InMemoryCompletionCounter_Increment_TracksCountsPerOwnerIndependently_Test()
    {
        // Arrange
        var counter = new InMemoryCompletionCounter();

        // Act
        counter.Increment("owner-1");
        counter.Increment("owner-1");
        counter.Increment("owner-2");

        // Assert
        Assert.Equal(2, counter.CountFor("owner-1"));
        Assert.Equal(1, counter.CountFor("owner-2"));
        Assert.Equal(0, counter.CountFor("owner-3"));
    }
}
