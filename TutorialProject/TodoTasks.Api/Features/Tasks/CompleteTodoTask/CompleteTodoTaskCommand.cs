namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Marks a <see cref="TodoTask"/> complete.
/// </summary>
/// <param name="Id">The task to complete.</param>
/// <param name="RequestedByUserId">
/// The authenticated caller's id - filled in by the endpoint, never trusted from the request body.
/// Only the task's owner may complete it - see <see cref="CompleteTodoTaskHandler"/>.
/// </param>
/// <remarks>
/// A four-case outcome: this command needs both the owner-scoping <see cref="Forbidden"/> case
/// <c>Features/Tasks/UpdateTodoTask</c> also has, and a <see cref="Conflict"/> case for the
/// domain-invalid state transition (already complete) that <c>UpdateTodoTaskCommand</c> has no
/// equivalent of. <c>ResourceCommandHandler{TCommand,TEntity,TResponse}</c> is generic over
/// <c>TResponse</c>'s cases beyond the required <see cref="NotFound"/>/<see cref="Forbidden"/> pair, so
/// <see cref="CompleteTodoTaskHandler"/> is still built on that base class despite the extra
/// <see cref="Conflict"/> case - see its own remarks.
/// </remarks>
public sealed record CompleteTodoTaskCommand(int Id, string RequestedByUserId)
    : ICommand<Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "complete_todo_task";
}
