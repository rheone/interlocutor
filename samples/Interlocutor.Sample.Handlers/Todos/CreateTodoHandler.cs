namespace Interlocutor.Sample.Handlers.Todos;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Creates the <see cref="TodoItem"/>. Duplicate-title rejection already happened in
/// <see cref="DuplicateTitleGuardStage"/> before this handler runs - all that's left here is the one
/// validation rule the stage doesn't own (a non-empty title) and the insert itself.
/// </summary>
/// <param name="db">The sample's database context.</param>
/// <remarks>
/// Does not call <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
/// itself - the EF Core <c>UnitOfWorkBehavior</c> does that after this handler returns a success outcome, as
/// part of committing the transaction it opened. That transaction begins on this method's own write
/// regardless of <see cref="CreateTodoCommand"/>'s marker; the marker's job is instead making that same
/// transaction cover <see cref="DuplicateTitleGuardStage"/>'s earlier read too - see its remarks.
/// </remarks>
public sealed class CreateTodoHandler(SampleDbContext db)
    : ICommandHandler<CreateTodoCommand, OperationOutcome<TodoItem, ValidationErrors, Conflict>>
{
    /// <inheritdoc/>
    public Task<OperationOutcome<TodoItem, ValidationErrors, Conflict>> Handle(CreateTodoCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Task.FromResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>(
                new ValidationErrors(["Title is required."])
            );
        }

        var todo = new TodoItem { Title = command.Title.Trim() };
        db.TodoItems.Add(todo);
        return Task.FromResult<OperationOutcome<TodoItem, ValidationErrors, Conflict>>(todo);
    }
}
