namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Lists the authenticated caller's own <see cref="TodoTask"/>s, optionally filtered to incomplete ones.</summary>
/// <param name="OnlyIncomplete">When <see langword="true"/>, only tasks not yet completed are returned.</param>
/// <param name="OwnerId">
/// The authenticated caller's id - filled in by the endpoint from <c>IRequestContextAccessor</c>, never
/// trusted from the request itself. Scopes the result to this caller's own tasks only.
/// </param>
public sealed record ListTodoTasksQuery(bool OnlyIncomplete, string OwnerId)
    : IQuery<Outcome<IReadOnlyList<TodoTaskDto>, Failure>>;
