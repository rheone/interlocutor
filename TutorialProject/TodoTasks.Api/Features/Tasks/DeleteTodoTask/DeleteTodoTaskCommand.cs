namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Deletes a <see cref="TodoTask"/>.
/// </summary>
/// <param name="Id">The task to delete.</param>
/// <remarks>
/// <see cref="IRequiresPolicy"/> is the coarse-grained authorization style: <c>AuthorizationBehavior</c>
/// evaluates <see cref="PolicyName"/> via <see cref="TodoTasksPolicyEvaluator"/> - which, for this
/// policy, loads the task itself to compare its owner against the caller - *before* the pipeline ever
/// reaches this command's own handler. Contrast with
/// <c>Features/Tasks/UpdateTodoTask/UpdateTodoTaskCommand</c> (ticket 07) and
/// <c>Features/Tasks/CompleteTodoTask</c>/<c>ReopenTodoTask</c> (ticket 08), which use
/// <c>ResourceCommandHandler</c>'s fine-grained, entity-based ownership check instead - those can only
/// run *after* loading, inside the handler's own template, since <c>ResourceCommandHandler</c>'s
/// <c>Authorize</c> hook inspects the already-loaded entity. Here, the same "is this caller the task's
/// owner" question is answered by the pipeline instead, before <see cref="DeleteTodoTaskHandler"/> runs
/// at all.
/// </remarks>
public sealed record DeleteTodoTaskCommand(int Id)
    : ICommand<Outcome<Success, NotFound, Forbidden>>,
        IRequiresPolicy,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string PolicyName => TodoTasksPolicyEvaluator.OwnsTaskPolicyName;

    /// <inheritdoc/>
    public string ActionName => "delete_todo_task";
}
