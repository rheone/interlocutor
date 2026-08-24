namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Looks up a single to-do item by its primary key. Read-only - never participates in a transaction.</summary>
/// <param name="Id">The to-do item's primary key.</param>
public sealed record GetTodoByIdQuery(int Id) : IQuery<OperationOutcome<TodoItem, NotFound>>;
