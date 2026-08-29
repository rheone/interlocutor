namespace TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="ClearCompletedTodoTasksCommand"/>. Registered against the specific
/// <c>TResult</c> this handler produces - this one only ever needs <see cref="Success"/> or
/// <see cref="Failure"/>, which <see cref="OperationOutcome"/> (the non-generic union) covers.
/// </summary>
public sealed class ClearCompletedTodoTasksHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : IVoidCommandHandler<ClearCompletedTodoTasksCommand, OperationOutcome>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome> Handle(ClearCompletedTodoTasksCommand command, CancellationToken ct)
    {
        var completed = await tasks.GetManyAsync(t => t.OwnerId == command.OwnerId && t.IsCompleted, cancellationToken: ct);

        foreach (var task in completed)
        {
            await tasks.DeleteAsync(task, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return default(Success);
    }
}
