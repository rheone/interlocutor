namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Notifications;

/// <summary>
/// Fired after a <see cref="TodoTasks.Api.Domain.TodoTask"/> is marked complete and that change has
/// been saved - but not yet committed; see <see cref="CompleteTodoTaskHandler"/>'s own remarks for why
/// publishing from inside the handler still runs before the actual commit. Two independent subscribers
/// react to this: <see cref="TodoTaskCompletionCounterHandler"/> and
/// <see cref="TodoTaskCompletionAuditLogHandler"/> - neither knows the other exists.
/// </summary>
/// <param name="TodoTaskId">The completed task's id.</param>
/// <param name="OwnerId">The completed task's owner.</param>
/// <param name="Title">The completed task's title, for logging without a second lookup.</param>
public sealed record TodoTaskCompletedNotification(int TodoTaskId, string OwnerId, string Title) : INotification;
