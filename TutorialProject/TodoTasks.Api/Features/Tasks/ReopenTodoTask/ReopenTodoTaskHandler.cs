namespace TodoTasks.Api.Features.Tasks.ReopenTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="ReopenTodoTaskCommand"/>.</summary>
public sealed class ReopenTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<ReopenTodoTaskCommand, OperationOutcome<TodoTaskDto, NotFound, Conflict>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<TodoTaskDto, NotFound, Conflict>> Handle(
        ReopenTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        if (!task.IsCompleted)
        {
            return new Conflict(Message: "This task isn't complete, so it can't be reopened.", Code: "not_completed");
        }

        task.IsCompleted = false;
        await unitOfWork.SaveChangesAsync(ct);

        return TodoTaskDto.FromEntity(task);
    }
}
