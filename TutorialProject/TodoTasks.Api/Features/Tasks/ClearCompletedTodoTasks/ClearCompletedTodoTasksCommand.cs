namespace TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;

/// <summary>
/// Deletes every completed <see cref="TodoTasks.Api.Domain.TodoTask"/> the caller owns. There's no
/// domain data to hand back on success, only whether it worked - a natural fit for the bare
/// <see cref="ICommand"/> marker (no <c>&lt;TResponse&gt;</c>) instead of <see cref="ICommand{TResponse}"/>,
/// which every other command in this project uses. Its handler contract is
/// <see cref="IVoidCommandHandler{TCommand,TResult}"/>, where <c>TResult</c> - any
/// <c>IOutcome</c>-implementing type - is chosen by the <em>caller</em>, via the type argument on
/// <c>ISender.Send&lt;TResult&gt;(ICommand, CancellationToken)</c>, not fixed by this command or baked
/// into the handler's registration. See the endpoint for where that type argument actually gets
/// supplied.
/// </summary>
/// <param name="OwnerId">
/// The authenticated caller's id - filled in by the endpoint from <c>IRequestContextAccessor</c>, never
/// trusted from the request itself.
/// </param>
public sealed record ClearCompletedTodoTasksCommand(string OwnerId) : ICommand, IAuditableRequest
{
    /// <inheritdoc/>
    public string ActionName => "clear_completed_todo_tasks";
}
