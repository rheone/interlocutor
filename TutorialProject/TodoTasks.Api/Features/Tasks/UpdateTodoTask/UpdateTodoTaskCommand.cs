namespace TodoTasks.Api.Features.Tasks.UpdateTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Updates an existing <see cref="TodoTask"/>'s title and notes.
/// </summary>
/// <param name="Id">The task to update.</param>
/// <param name="Title">The task's new title.</param>
/// <param name="Notes">The task's new notes.</param>
/// <param name="RequestedByUserId">
/// The authenticated caller's id - filled in by the endpoint, never trusted from the request body.
/// Only the task's owner may update it - see <see cref="UpdateTodoTaskHandler.Authorize"/>.
/// </param>
/// <remarks>
/// No FluentValidation here, deliberately: this command's own response shape,
/// <c>Outcome{TodoTaskDto,NotFound,Forbidden}</c>, has no <see cref="ValidationErrors"/> case for
/// <c>ValidationBehavior</c> to construct on a validation failure - registering a validator for this
/// shape is a build-time <c>INTERLOC010</c> diagnostic, not a runtime surprise. Input checks belong in
/// <see cref="UpdateTodoTaskHandler.Authorize"/> or its <c>HandleAsync</c> instead.
/// </remarks>
public sealed record UpdateTodoTaskCommand(int Id, string Title, string? Notes, string RequestedByUserId)
    : ICommand<Outcome<TodoTaskDto, NotFound, Forbidden>>,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "update_todo_task";
}
