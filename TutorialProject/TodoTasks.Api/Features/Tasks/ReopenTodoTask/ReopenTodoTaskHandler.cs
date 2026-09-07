namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="ReopenTodoTaskCommand"/> - the mirror image of
/// <c>Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler</c>, without the notification: reopening
/// isn't the event the guide's completion-count/audit-log subscribers care about, and nothing else in
/// this project needs to react to it, so there's no <c>TodoTaskReopenedNotification</c> to publish.
/// </summary>
public sealed class ReopenTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<ReopenTodoTaskCommand, Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>
{
    /// <inheritdoc/>
    public async Task<Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>> Handle(
        ReopenTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        if (task.OwnerId != command.RequestedByUserId)
        {
            return new Forbidden("Only the task's owner can reopen it.");
        }

        if (!task.IsCompleted)
        {
            return new Conflict(Message: "This task isn't complete, so it can't be reopened.", Code: "not_completed");
        }

        task.IsCompleted = false;
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return TodoTaskDto.FromEntity(task);
    }
}
