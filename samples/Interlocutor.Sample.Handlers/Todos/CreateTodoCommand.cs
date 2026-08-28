namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Creates a new to-do item. A value-returning command: it mutates state and hands back the created entity,
/// not just a bare outcome. Implements <see cref="ITransactionalRequest"/> so <see cref="DuplicateTitleGuardStage"/>'s
/// duplicate-title check and the handler's insert that follows it share one eager transaction from pipeline
/// start, opened by the EF Core <c>UnitOfWorkBehavior</c> - closing the race a lazy, begin-on-first-write
/// transaction would otherwise leave between the check and the write.
/// </summary>
/// <param name="Title">The new item's title. Empty or whitespace-only fails validation.</param>
public sealed record CreateTodoCommand(string Title)
    : ICommand<OperationOutcome<TodoItem, ValidationErrors, Conflict>>,
        ITransactionalRequest;
