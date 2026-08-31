namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Reopens a completed <see cref="TodoTask"/> - the mirror image of
/// <c>Features/Tasks/CompleteTodoTask/CompleteTodoTaskCommand</c> (<see cref="Conflict"/> when the
/// task *isn't* completed, no notification fired).
/// </summary>
/// <param name="Id">The task to reopen.</param>
public sealed record ReopenTodoTaskCommand(int Id) : ICommand<Outcome<TodoTaskDto, NotFound, Conflict>>, IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "reopen_todo_task";
}
