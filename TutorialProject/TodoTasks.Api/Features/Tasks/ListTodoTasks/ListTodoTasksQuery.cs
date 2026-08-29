namespace TodoTasks.Api.Features.Tasks.ListTodoTasks;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Lists every <see cref="TodoTask"/>, optionally filtered to incomplete ones.</summary>
/// <param name="OnlyIncomplete">When <see langword="true"/>, only tasks not yet completed are returned.</param>
public sealed record ListTodoTasksQuery(bool OnlyIncomplete) : IQuery<OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>>;
