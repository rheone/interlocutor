namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;

/// <summary>
/// Deletes a to-do item. A unit command: it mutates state but returns no domain data - only an outcome.
/// Transactional, same as <see cref="CreateTodoCommand"/>.
/// </summary>
/// <param name="Id">The to-do item's primary key.</param>
public sealed record DeleteTodoCommand(int Id) : ICommand, ITransactionalRequest;
