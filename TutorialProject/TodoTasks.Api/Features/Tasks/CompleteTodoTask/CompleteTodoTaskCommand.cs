namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Marks a <see cref="TodoTask"/> complete.</summary>
/// <param name="Id">The task to complete.</param>
public sealed record CompleteTodoTaskCommand(int Id) : ICommand<Outcome<TodoTaskDto, NotFound, Conflict>>, IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "complete_todo_task";
}
