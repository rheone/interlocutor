namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="CreateTodoTaskCommand"/>.</summary>
public sealed class CreateTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTodoTaskCommand, OperationOutcome<TodoTaskDto, ValidationErrors, Conflict>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<TodoTaskDto, ValidationErrors, Conflict>> Handle(
        CreateTodoTaskCommand command,
        CancellationToken ct
    )
    {
        // Shares CreateTodoTaskCommand's ITransactionalRequest-eager transaction with the insert below
        // - see that command's own remarks for why this specific ordering needs one.
        var hasDuplicateTitle =
            (
                await tasks.GetManyAsync(t => t.OwnerId == command.OwnerId && t.Title == command.Title, cancellationToken: ct)
            ).Count > 0;

        if (hasDuplicateTitle)
        {
            // Named arguments deliberately: Conflict's constructor is (Message, Code), not (Code, Message)
            // - see this ticket's own notes for the mix-up this avoids.
            return new Conflict(Message: $"You already have a task titled '{command.Title}'.", Code: "duplicate_title");
        }

        var task = new TodoTask(command.Title, command.OwnerId, command.Notes);
        await tasks.InsertAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct); // UnitOfWorkBehavior commits from here on success.

        return TodoTaskDto.FromEntity(task);
    }
}
