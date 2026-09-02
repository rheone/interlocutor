namespace TodoTasks.Api.Features.Tasks.GetTodoTaskById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Looks up one <see cref="TodoTask"/> by id.</summary>
/// <param name="Id">The task's identifier.</param>
public sealed record GetTodoTaskByIdQuery(int Id) : IQuery<Outcome<TodoTaskDto, NotFound>>;
