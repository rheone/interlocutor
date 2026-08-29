namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Deletes a <see cref="TodoTask"/>.
/// </summary>
/// <param name="Id">The task to delete.</param>
/// <remarks>
/// <see cref="IRequiresPolicy"/> is the coarse-grained authorization style: <c>AuthorizationBehavior</c>
/// evaluates <see cref="PolicyName"/> via <c>Infrastructure/TodoTaskPolicyEvaluator</c> *before* this
/// entity is ever loaded - "can this caller attempt this kind of command at all," not "is this
/// specific task theirs." Contrast with
/// <c>Features/Tasks/UpdateTodoTask/UpdateTodoTaskCommand</c> (ticket 07), which uses
/// <c>ResourceCommandHandler</c>'s fine-grained, entity-based ownership check instead - that one can
/// only run *after* loading, since it needs to inspect the loaded entity's own state.
/// </remarks>
public sealed record DeleteTodoTaskCommand(int Id)
    : ICommand<OperationOutcome<Success, NotFound, Forbidden>>,
        IRequiresPolicy,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string PolicyName => "DeleteTodoTask";

    /// <inheritdoc/>
    public string ActionName => "delete_todo_task";
}
