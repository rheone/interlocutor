namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Creates a new <see cref="TodoTask"/>.
/// </summary>
/// <param name="Title">The new task's title - must be unique among the caller's own tasks.</param>
/// <param name="Notes">Optional free-text notes.</param>
/// <param name="OwnerId">
/// The authenticated caller's id - filled in by the endpoint from <c>IRequestContextAccessor</c>, never
/// trusted from the request body.
/// </param>
/// <remarks>
/// <see cref="ITransactionalRequest"/> is here for the same reason
/// <c>documentation/beginners-guide-todo-api.md</c> Step 5 states for this exact command: the
/// duplicate-title read in <see cref="CreateTodoTaskHandler"/> and the insert that follows it need to
/// share one transaction, or two concurrent requests for the same title could both pass the check
/// before either commits. <see cref="IAuditableRequest"/> records this action via
/// <c>AuditingBehavior</c> - see <see cref="ActionName"/>.
/// </remarks>
public sealed record CreateTodoTaskCommand(string Title, string? Notes, string OwnerId)
    : ICommand<Outcome<TodoTaskDto, ValidationErrors, Conflict>>,
        ITransactionalRequest,
        IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "create_todo_task";
}
