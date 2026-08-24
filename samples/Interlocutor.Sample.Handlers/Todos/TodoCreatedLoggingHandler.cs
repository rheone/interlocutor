namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

/// <summary>Logs every created to-do item. One of two independent subscribers to <see cref="TodoCreatedNotification"/>.</summary>
/// <param name="logger">The logger to write to.</param>
public sealed class TodoCreatedLoggingHandler(ILogger<TodoCreatedLoggingHandler> logger)
    : INotificationHandler<TodoCreatedNotification>
{
    /// <inheritdoc/>
    public Task Handle(TodoCreatedNotification notification, CancellationToken ct)
    {
        logger.LogInformation("To-do {TodoId} created: {Title}", notification.TodoId, notification.Title);
        return Task.CompletedTask;
    }
}
