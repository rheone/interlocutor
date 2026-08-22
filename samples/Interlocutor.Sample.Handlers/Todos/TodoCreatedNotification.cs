namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Notifications;

/// <summary>
/// Published after a <see cref="CreateTodoCommand"/> commits successfully. Two independent subscribers
/// react to it - <see cref="TodoCreatedLoggingHandler"/> and <see cref="TodoCreatedCounterHandler"/> - neither
/// aware of the other, demonstrating fan-out to more than one handler for the same notification.
/// </summary>
/// <param name="TodoId">The created item's primary key.</param>
/// <param name="Title">The created item's title.</param>
public sealed record TodoCreatedNotification(int TodoId, string Title) : INotification;
