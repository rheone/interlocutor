namespace TodoTasks.Api.Features.Tasks.CompleteTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="CompleteTodoTaskCommand"/> via the load/authorize/handle template
/// (<c>ResourceCommandHandler{TCommand,TEntity,TResponse}</c>) - an owner-scoped state transition that can
/// also fail for a domain reason, and publishes a notification about it.
/// </summary>
/// <remarks>
/// <para>
/// Built on <c>ResourceCommandHandler{TCommand,TEntity,TResponse}</c> despite this command's four-case
/// response: unlike the fixed three-case <c>Outcome&lt;_, NotFound, Forbidden&gt;</c> shape an earlier
/// revision of that base class had, it is generic over <c>TResponse</c>'s cases beyond the required
/// <see cref="NotFound"/>/<see cref="Forbidden"/> pair, so <see cref="Conflict"/> - the domain-invalid
/// state transition (already complete) - fits as an ordinary <see cref="HandleAsync(CompleteTodoTaskCommand,TodoTask,CancellationToken)"/>
/// return alongside the template's own <see cref="NotFound"/>/<see cref="Forbidden"/> short-circuits.
/// </para>
/// <para>
/// The explicit <see cref="ICommandHandler{TCommand,TResponse}"/> declaration below is required, not
/// decorative: <c>ResourceCommandHandler{TCommand,TEntity,TResponse}</c> does not implement it itself, so
/// <c>ScanAssembly</c>'s handler discovery never finds a plain <c>ResourceCommandHandler</c> subclass on its
/// own - every dispatch to <see cref="CompleteTodoTaskCommand"/> would throw "service not registered"
/// without this. The base class's own public <c>HandleAsync</c> method already matches the interface's
/// signature exactly, so no override body is needed here - the declaration alone is enough.
/// </para>
/// </remarks>
public sealed class CompleteTodoTaskHandler(
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork,
    IPublisher publisher
)
    : ResourceCommandHandler<CompleteTodoTaskCommand, TodoTask, Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>,
        ICommandHandler<CompleteTodoTaskCommand, Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>>
{
    /// <summary>Loads the task to complete. A <see langword="null"/> result short-circuits with <see cref="NotFound"/> before <see cref="Authorize"/> ever runs.</summary>
    /// <param name="command">The complete command, carrying the task's id.</param>
    /// <param name="ct">A token to observe while loading.</param>
    /// <returns>The task to complete, or <see langword="null"/> if none exists with that id.</returns>
    protected override Task<TodoTask?> LoadAsync(CompleteTodoTaskCommand command, CancellationToken ct) =>
        Task.FromResult(tasks.GetById(command.Id));

    /// <summary>Authorizes against the *loaded* entity's own state - the ownership check <c>AuthorizationBehavior</c> cannot express, since it runs before anything is loaded.</summary>
    /// <param name="entity">The loaded task.</param>
    /// <param name="command">The complete command, carrying the requesting caller's id.</param>
    /// <returns>A <see cref="Forbidden"/> if the caller isn't the task's owner; otherwise <see langword="null"/>.</returns>
    protected override Forbidden? Authorize(TodoTask entity, CompleteTodoTaskCommand command) =>
        entity.OwnerId == command.RequestedByUserId ? null : new Forbidden("Only the task's owner can complete it.");

    /// <summary>
    /// Marks the loaded, authorized task complete, unless it already is.
    /// </summary>
    /// <remarks>
    /// Persist first, publish second - but publishing from inside this method still runs
    /// <see cref="TodoTaskCompletedNotification"/>'s subscribers *before* the completion actually
    /// commits, not after: <c>UnitOfWorkBehavior</c> only commits once this <c>HandleAsync</c> method
    /// returns to the pipeline, so a subscriber that throws here unwinds past that commit entirely,
    /// leaving the completion rolled back rather than safely decoupled from it. See
    /// <c>documentation/transactions-and-store-integration.md</c> "Transactions with Notifications" for
    /// the full mechanism, and for the pattern that actually publishes after commit (publish from the
    /// caller of <c>Send</c>, once it returns, instead of from inside the handler). Kept as an
    /// in-handler publish here anyway, matching <c>documentation/beginners-guide-todo-api.md</c> Step
    /// 8's own example - this is the guide's own worked example of that trade-off, not a fix applied
    /// only to this project.
    /// </remarks>
    /// <param name="command">The complete command.</param>
    /// <param name="entity">The loaded, authorized task.</param>
    /// <param name="ct">A token to observe while saving and publishing.</param>
    /// <returns>The completed task's response shape, or <see cref="Conflict"/> if it was already complete.</returns>
    protected override async Task<Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>> HandleAsync(
        CompleteTodoTaskCommand command,
        TodoTask entity,
        CancellationToken ct
    )
    {
        if (entity.IsCompleted)
        {
            return new Conflict(Message: "This task is already complete.", Code: "already_completed");
        }

        entity.IsCompleted = true;
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await publisher
            .PublishAsync(new TodoTaskCompletedNotification(entity.Id, entity.OwnerId, entity.Title), ct)
            .ConfigureAwait(false);

        return TodoTaskDto.FromEntity(entity);
    }
}
