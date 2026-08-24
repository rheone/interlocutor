namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Creates a new to-do item. A value-returning command: it mutates state and hands back the created entity,
/// not just a bare outcome. Transactional - <see cref="DuplicateTitleGuardStage"/> checks for a duplicate
/// title before the handler runs, and the handler's insert is committed or rolled back as a unit by the
/// EF Core <c>TransactionBehavior</c> because this command implements <see cref="ITransactionalRequest"/>.
/// </summary>
/// <param name="Title">The new item's title. Empty or whitespace-only fails validation.</param>
public sealed record CreateTodoCommand(string Title)
    : ICommand<OperationOutcome<TodoItem, ValidationErrors, Conflict>>,
        ITransactionalRequest;
