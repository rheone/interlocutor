namespace TodoTasks.Api.Features.Tasks.DeleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="DeleteTodoTaskCommand"/>. By the time this runs, <c>AuthorizationBehavior</c> has
/// already evaluated <see cref="Infrastructure.TodoTasksPolicyEvaluator"/> and confirmed the caller owns
/// the task (or the task didn't exist, in which case the policy evaluator lets the request through so
/// this handler - not the policy check - produces <see cref="NotFound"/>). This handler therefore only
/// needs to worry about whether the task still exists by the time it runs.
/// </summary>
public sealed class DeleteTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTodoTaskCommand, Outcome<Success, NotFound, Forbidden>>
{
    /// <inheritdoc/>
    public Task<Outcome<Success, NotFound, Forbidden>> Handle(DeleteTodoTaskCommand command, CancellationToken ct) =>
        this.HandleAsync(command, ct);

    /// <inheritdoc/>
    public async Task<Outcome<Success, NotFound, Forbidden>> HandleAsync(DeleteTodoTaskCommand command, CancellationToken ct)
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        await tasks.DeleteAsync(task, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return default(Success);
    }
}
