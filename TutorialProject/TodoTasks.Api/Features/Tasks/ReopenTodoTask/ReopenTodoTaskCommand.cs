namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Reopens a completed <see cref="TodoTask"/> - the mirror image of
/// <c>Features/Tasks/CompleteTodoTask/CompleteTodoTaskCommand</c> (<see cref="Conflict"/> when the task
/// *isn't* completed, no notification fired - see <see cref="ReopenTodoTaskHandler"/>'s own remarks for
/// why).
/// </summary>
/// <param name="Id">The task to reopen.</param>
/// <param name="RequestedByUserId">
/// The authenticated caller's id - filled in by the endpoint, never trusted from the request body.
/// Only the task's owner may reopen it - see <see cref="ReopenTodoTaskHandler"/>.
/// </param>
public sealed record ReopenTodoTaskCommand(int Id, string RequestedByUserId)
    : ICommand<Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "reopen_todo_task";
}
