namespace TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="ClearCompletedTodoTasksCommand"/>. Registered against the specific <c>TResult</c> this
/// handler produces - clearing every completed task for an owner is genuinely infallible (there is no
/// not-found/conflict/validation case it can hit), so it uses the case-less, non-generic <see cref="Outcome"/>
/// acknowledgment rather than naming a failure case that could never actually construct.
/// </summary>
public sealed class ClearCompletedTodoTasksHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : IBareCommandHandler<ClearCompletedTodoTasksCommand, Outcome>
{
    /// <inheritdoc/>
    public async Task<Outcome> Handle(ClearCompletedTodoTasksCommand command, CancellationToken ct)
    {
        var completed = await tasks
            .GetManyAsync(t => t.OwnerId == command.OwnerId && t.IsCompleted, cancellationToken: ct)
            .ConfigureAwait(false);

        foreach (var task in completed)
        {
            await tasks.DeleteAsync(task, ct).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return default(Outcome);
    }
}
