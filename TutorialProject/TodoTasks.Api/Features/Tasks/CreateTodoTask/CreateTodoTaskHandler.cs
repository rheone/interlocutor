namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="CreateTodoTaskCommand"/>.</summary>
public sealed class CreateTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTodoTaskCommand, Outcome<TodoTaskDto, ValidationErrors, Conflict>>
{
    /// <inheritdoc/>
    public async Task<Outcome<TodoTaskDto, ValidationErrors, Conflict>> Handle(
        CreateTodoTaskCommand command,
        CancellationToken ct
    )
    {
        // Shares CreateTodoTaskCommand's ITransactionalRequest-eager transaction with the insert below -
        // see that command's own remarks for why this specific ordering needs one. Scoped per-owner
        // (OwnerId and Title both), not globally - the same caller can't have two tasks with the same
        // title, but two different callers can each have their own "Buy milk", matching the shelved
        // NHibernate build's own semantics for this check. TodoTasksDbContext also declares a unique
        // index on (OwnerId, Title) as a schema-level backstop - the outcome the caller sees comes from
        // this pre-check, not from catching and translating a constraint-violation exception.
        var hasDuplicateTitle =
            (
                await tasks
                    .GetManyAsync(t => t.OwnerId == command.OwnerId && t.Title == command.Title, cancellationToken: ct)
                    .ConfigureAwait(false)
            ).Count > 0;

        if (hasDuplicateTitle)
        {
            return new Conflict(Message: $"You already have a task titled '{command.Title}'.", Code: "duplicate_title");
        }

        var task = new TodoTask(command.Title, command.OwnerId, command.Notes);
        await tasks.InsertAsync(task, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false); // UnitOfWorkBehavior commits from here on success.

        return TodoTaskDto.FromEntity(task);
    }
}
