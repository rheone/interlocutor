namespace TodoTasks.Api.Features.Tasks.UpdateTodoTask;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="UpdateTodoTaskCommand"/> via the load/authorize/handle template
/// (<c>ResourceCommandHandler{TCommand,TEntity,TSuccess}</c>). Contrast this with
/// <c>Features/Tasks/DeleteTodoTask/DeleteTodoTaskHandler</c>'s coarse, non-resource
/// <c>IRequiresPolicy</c> check (ticket 09) - that runs *before* anything is loaded and asks "can
/// this caller attempt this kind of command at all"; this one runs *after* loading and asks "is this
/// specific task theirs."
/// </summary>
/// <remarks>
/// The explicit <see cref="ICommandHandler{TCommand,TResponse}"/> declaration below is required, not
/// decorative: <c>ResourceCommandHandler{TCommand,TEntity,TSuccess}</c> does not implement it itself
/// (confirmed against the shipped source, not assumed), so <c>ScanAssembly</c>'s handler discovery
/// never finds a plain <c>ResourceCommandHandler</c> subclass on its own - every dispatch to
/// <see cref="UpdateTodoTaskCommand"/> would throw "service not registered" without this. The base
/// class's own public <c>Handle</c> method already matches the interface's signature exactly, so no
/// override body is needed here - the declaration alone is enough.
/// </remarks>
public sealed class UpdateTodoTaskHandler(IDomainRepository<TodoTask, int> tasks, IUnitOfWork unitOfWork)
    : ResourceCommandHandler<UpdateTodoTaskCommand, TodoTask, TodoTaskDto>,
        ICommandHandler<UpdateTodoTaskCommand, OperationOutcome<TodoTaskDto, NotFound, Failure>>
{
    /// <summary>Loads the task to update. A <see langword="null"/> result short-circuits with <see cref="NotFound"/> before <see cref="Authorize"/> ever runs.</summary>
    /// <param name="command">The update command, carrying the task's id.</param>
    /// <param name="ct">A token to observe while loading.</param>
    /// <returns>The task to update, or <see langword="null"/> if none exists with that id.</returns>
    protected override Task<TodoTask?> LoadAsync(UpdateTodoTaskCommand command, CancellationToken ct) =>
        Task.FromResult(tasks.GetById(command.Id));

    /// <summary>Authorizes against the *loaded* entity's own state - the ownership check <c>AuthorizationBehavior</c> cannot express, since it runs before anything is loaded.</summary>
    /// <param name="entity">The loaded task.</param>
    /// <param name="command">The update command, carrying the requesting caller's id.</param>
    /// <returns>A <see cref="Failure"/> if the caller isn't the task's owner; otherwise <see langword="null"/>.</returns>
    protected override Failure? Authorize(TodoTask entity, UpdateTodoTaskCommand command) =>
        entity.OwnerId == command.RequestedByUserId ? null : new Failure("Only the task's owner can update it.");

    /// <summary>Applies the update. The entity is already loaded and authorized by the time this runs.</summary>
    /// <param name="command">The update command, carrying the new title/notes.</param>
    /// <param name="entity">The loaded, authorized task.</param>
    /// <param name="ct">A token to observe while saving.</param>
    /// <returns>The updated task's response shape.</returns>
    protected override async Task<OperationOutcome<TodoTaskDto, NotFound, Failure>> HandleAsync(
        UpdateTodoTaskCommand command,
        TodoTask entity,
        CancellationToken ct
    )
    {
        entity.Title = command.Title;
        entity.Notes = command.Notes;
        await unitOfWork.SaveChangesAsync(ct);

        return TodoTaskDto.FromEntity(entity);
    }
}
