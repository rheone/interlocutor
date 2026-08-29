namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Notifications;

/// <summary>The other of two independent subscribers to <see cref="TodoTaskCompletedNotification"/> - writes a log entry.</summary>
public sealed class TodoTaskCompletionAuditLogHandler(ILogger<TodoTaskCompletionAuditLogHandler> logger)
    : INotificationHandler<TodoTaskCompletedNotification>
{
    /// <inheritdoc/>
    public Task Handle(TodoTaskCompletedNotification notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Task {TodoTaskId} ('{Title}') completed by {OwnerId}",
            notification.TodoTaskId,
            notification.Title,
            notification.OwnerId
        );
        return Task.CompletedTask;
    }
}
