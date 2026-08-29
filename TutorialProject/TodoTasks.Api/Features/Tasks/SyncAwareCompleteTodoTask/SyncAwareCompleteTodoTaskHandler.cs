namespace TodoTasks.Api.Features.Tasks.SyncAwareCompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Handles <see cref="SyncAwareCompleteTodoTaskCommand"/> - a variant of
/// <c>CompleteTodoTask.CompleteTodoTaskHandler</c> that forces a rollback via <c>IUnitOfWork.Cancel</c>
/// instead of committing, when an external system already completed the same task independently.
/// </summary>
/// <remarks>
/// <para>
/// This is a distinct command/handler pair from <c>CompleteTodoTask.CompleteTodoTaskCommand</c> rather
/// than a second handler registered for it: two closed handlers matching the same request/response pair
/// is a compile-time diagnostic (<c>INTERLOC002</c>), not a runtime choice, and there is no
/// <c>[HandlerOverride]</c> escape hatch for that case - see
/// <c>documentation/handler-selection-and-dispatch.md</c>. <c>documentation/beginners-guide-todo-api.md</c>
/// Step 8's own illustrative example makes the same point by never actually compiling this alongside the
/// real handler - "it would replace, not join, <c>CompleteTodoTaskHandler</c>."
/// </para>
/// <para>
/// Deliberately not the project's real completion path, and not wired into <c>Program.cs</c> - a rare-case
/// tool this guide demonstrates, not something every write handler needs. See
/// <c>documentation/transactions-and-store-integration.md</c> § Cancel for the full semantics.
/// </para>
/// </remarks>
public sealed class SyncAwareCompleteTodoTaskHandler(
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork,
    IExternalTaskSyncClient externalSync
) : ICommandHandler<SyncAwareCompleteTodoTaskCommand, OperationOutcome<TodoTaskDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<TodoTaskDto, NotFound>> Handle(
        SyncAwareCompleteTodoTaskCommand command,
        CancellationToken ct
    )
    {
        if (tasks.GetById(command.Id) is not { } task)
        {
            return new NotFound($"TodoTask {command.Id}");
        }

        task.IsCompleted = true; // Staged - not committed until SaveChangesAsync or pipeline egress.

        if (await externalSync.WasAlreadyCompletedElsewhereAsync(task.Id, ct))
        {
            // The external system already completed this task - abandon our own staged write rather
            // than double-apply it. This still isn't our failure: the task ends up completed either
            // way, so report success, not a failure case. Unlike CompleteTodoTaskCommand (whose response
            // carries Conflict for an already-completed task), this command never checks task.IsCompleted
            // itself - completing an already-complete task through this path is not an error to guard
            // against, so its response shape only needs NotFound.
            await unitOfWork.Cancel(ct);
            return TodoTaskDto.FromEntity(task);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return TodoTaskDto.FromEntity(task);
    }
}
