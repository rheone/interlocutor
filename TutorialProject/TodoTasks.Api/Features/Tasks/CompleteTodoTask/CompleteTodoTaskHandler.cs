namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="CompleteTodoTaskCommand"/> - a state transition that can fail for a domain reason, and fires a notification once it commits.</summary>
public sealed class CompleteTodoTaskHandler(
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork,
    IPublisher publisher
) : ICommandHandler<CompleteTodoTaskCommand, OperationOutcome<TodoTaskDto, NotFound, Conflict>>
{
    /// <inheritdoc/>
    /// <remarks>
    /// Persist first, publish second, deliberately: <see cref="TodoTaskCompletedNotification"/>'s
    /// subscribers run after this handler's own transaction has already committed, so a failure in a
    /// subscriber can never roll back the completion itself. See
    /// <c>documentation/transactions-and-store-integration.md</c> "Transactions with Notifications" for
    /// the full reasoning.
    /// </remarks>
    public async Task<OperationOutcome<TodoTaskDto, NotFound, Conflict>> Handle(
        CompleteTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        if (task.IsCompleted)
        {
            return new Conflict(Message: "This task is already complete.", Code: "already_completed");
        }

        task.IsCompleted = true;
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.Publish(new TodoTaskCompletedNotification(task.Id, task.OwnerId, task.Title), ct);

        return TodoTaskDto.FromEntity(task);
    }
}
