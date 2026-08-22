namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Notifications;

/// <summary>Tallies created to-do items. The other of two independent subscribers to <see cref="TodoCreatedNotification"/>.</summary>
/// <param name="counter">The counter to increment.</param>
public sealed class TodoCreatedCounterHandler(ITodoCreationCounter counter) : INotificationHandler<TodoCreatedNotification>
{
    /// <inheritdoc/>
    public Task Handle(TodoCreatedNotification notification, CancellationToken ct)
    {
        counter.Increment();
        return Task.CompletedTask;
    }
}
