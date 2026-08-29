namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="DeleteTodoTaskCommand"/>. By the time this runs, <c>AuthorizationBehavior</c> has already confirmed the caller satisfies the <c>"DeleteTodoTask"</c> policy - this handler only needs to worry about whether the task exists.</summary>
public sealed class DeleteTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTodoTaskCommand, OperationOutcome<Success, NotFound, Forbidden>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<Success, NotFound, Forbidden>> Handle(
        DeleteTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        await tasks.DeleteAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return default(Success);
    }
}
