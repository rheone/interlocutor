namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="GetTodoTaskByIdQuery"/> - a plain read with no marker interfaces: no write means
/// no transaction needs to open for this request at all.
/// </summary>
/// <remarks>
/// Owner-scoped by construction: a task that exists but is owned by someone other than
/// <see cref="GetTodoTaskByIdQuery.OwnerId"/> falls through to the same <see cref="NotFound"/> case as
/// a task that doesn't exist at all, rather than a distinct <c>Forbidden</c> - returning <c>Forbidden</c>
/// for the "wrong owner" branch would let a caller distinguish "doesn't exist" from "exists, isn't
/// yours" purely from the response shape, leaking the id's existence to a non-owner.
/// </remarks>
public sealed class GetTodoTaskByIdHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<GetTodoTaskByIdQuery, Outcome<TodoTaskDto, NotFound>>
{
    /// <inheritdoc/>
    public Task<Outcome<TodoTaskDto, NotFound>> Handle(GetTodoTaskByIdQuery query, CancellationToken ct) =>
        this.HandleAsync(query, ct);

    /// <inheritdoc/>
    public Task<Outcome<TodoTaskDto, NotFound>> HandleAsync(GetTodoTaskByIdQuery query, CancellationToken ct) =>
        Task.FromResult<Outcome<TodoTaskDto, NotFound>>(
            tasks.GetById(query.Id) is { } task && task.OwnerId == query.OwnerId
                ? TodoTaskDto.FromEntity(task)
                : new NotFound($"TodoTask {query.Id}")
        );
}
