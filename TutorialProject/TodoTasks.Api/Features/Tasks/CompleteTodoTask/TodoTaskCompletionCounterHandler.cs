namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Notifications;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// One of two independent subscribers to <see cref="TodoTaskCompletedNotification"/> - increments the
/// completing owner's completion count.
/// </summary>
public sealed class TodoTaskCompletionCounterHandler(ICompletionCounter counter)
    : INotificationHandler<TodoTaskCompletedNotification>
{
    /// <inheritdoc/>
    public Task Handle(TodoTaskCompletedNotification notification, CancellationToken ct)
    {
        counter.Increment(notification.OwnerId);
        return Task.CompletedTask;
    }
}
