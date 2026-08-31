namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="GetTodoTaskByIdQuery"/> - a plain read with no marker interfaces: no write
/// means no transaction needs to open for this request at all.
/// </summary>
public sealed class GetTodoTaskByIdHandler(IDomainRepository<TodoTask, int> tasks)
    : IQueryHandler<GetTodoTaskByIdQuery, Outcome<TodoTaskDto, NotFound>>
{
    /// <inheritdoc/>
    public Task<Outcome<TodoTaskDto, NotFound>> Handle(GetTodoTaskByIdQuery query, CancellationToken ct) =>
        Task.FromResult<Outcome<TodoTaskDto, NotFound>>(
            tasks.GetById(query.Id) is { } task ? TodoTaskDto.FromEntity(task) : new NotFound($"TodoTask {query.Id}")
        );
}
