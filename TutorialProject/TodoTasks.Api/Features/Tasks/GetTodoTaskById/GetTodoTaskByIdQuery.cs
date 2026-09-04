namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Looks up one <see cref="TodoTask"/> by id, scoped to its owner.</summary>
/// <param name="Id">The task's identifier.</param>
/// <param name="OwnerId">
/// The authenticated caller's id - filled in by the endpoint from <c>IRequestContextAccessor</c>, never
/// trusted from the request itself. A task that exists but belongs to a different owner produces the
/// same <see cref="NotFound"/> outcome as a task that doesn't exist at all - see
/// <see cref="GetTodoTaskByIdHandler"/>'s remarks for why that's deliberate.
/// </param>
public sealed record GetTodoTaskByIdQuery(int Id, string OwnerId) : IQuery<Outcome<TodoTaskDto, NotFound>>;
