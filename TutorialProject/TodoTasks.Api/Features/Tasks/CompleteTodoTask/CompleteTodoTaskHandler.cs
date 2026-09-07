namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="CompleteTodoTaskCommand"/> - an owner-scoped state transition that can fail for a
/// domain reason, and publishes a notification about it.
/// </summary>
/// <remarks>
/// Not built on <c>ResourceCommandHandler{TCommand,TEntity,TSuccess}</c> - see
/// <see cref="CompleteTodoTaskCommand"/>'s own remarks for why its fixed
/// <c>Outcome{TSuccess,NotFound,Forbidden}</c> shape doesn't have room for the <see cref="Conflict"/>
/// case this handler also needs, so the load/authorize/handle steps that base class would otherwise
/// provide are inlined here instead.
/// </remarks>
public sealed class CompleteTodoTaskHandler(
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork,
    IPublisher publisher
) : ICommandHandler<CompleteTodoTaskCommand, Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>
{
    /// <inheritdoc/>
    /// <remarks>
    /// Persist first, publish second - but publishing from inside this method still runs
    /// <see cref="TodoTaskCompletedNotification"/>'s subscribers *before* the completion actually
    /// commits, not after: <c>UnitOfWorkBehavior</c> only commits once this <c>Handle</c> method
    /// returns to the pipeline, so a subscriber that throws here unwinds past that commit entirely,
    /// leaving the completion rolled back rather than safely decoupled from it. See
    /// <c>documentation/transactions-and-store-integration.md</c> "Transactions with Notifications" for
    /// the full mechanism, and for the pattern that actually publishes after commit (publish from the
    /// caller of <c>Send</c>, once it returns, instead of from inside the handler). Kept as an
    /// in-handler publish here anyway, matching <c>documentation/beginners-guide-todo-api.md</c> Step
    /// 8's own example - this is the guide's own worked example of that trade-off, not a fix applied
    /// only to this project.
    /// </remarks>
    public async Task<Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>> Handle(
        CompleteTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        if (task.OwnerId != command.RequestedByUserId)
        {
            return new Forbidden("Only the task's owner can complete it.");
        }

        if (task.IsCompleted)
        {
            return new Conflict(Message: "This task is already complete.", Code: "already_completed");
        }

        task.IsCompleted = true;
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await publisher.Publish(new TodoTaskCompletedNotification(task.Id, task.OwnerId, task.Title), ct).ConfigureAwait(false);

        return TodoTaskDto.FromEntity(task);
    }
}
