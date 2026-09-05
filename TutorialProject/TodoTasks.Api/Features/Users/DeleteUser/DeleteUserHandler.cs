namespace TodoTasks.Api.Features.Users.DeleteUser;

using System.Globalization;
using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="DeleteUserCommand"/>. This project's own worked example of a single
/// <see cref="IUnitOfWork"/> spanning two repositories - see
/// <c>documentation/beginners-guide-todo-api.md</c>'s "Bonus: multiple repositories, one unit of work"
/// section for the fuller explanation this handler makes real: the user and every task they own are
/// deleted together or not at all, because both repositories below are built over the same scoped EF
/// Core context that one <see cref="IUnitOfWork.SaveChangesAsync"/> call commits.
/// </summary>
public sealed class DeleteUserHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteUserCommand, Outcome<Success, NotFound>>
{
    /// <inheritdoc/>
    public Task<Outcome<Success, NotFound>> Handle(DeleteUserCommand command, CancellationToken ct) =>
        this.HandleAsync(command, ct);

    /// <inheritdoc/>
    public async Task<Outcome<Success, NotFound>> HandleAsync(DeleteUserCommand command, CancellationToken ct)
    {
        if (users.GetById(command.Id) is not { } user)
        {
            return new NotFound($"User {command.Id}");
        }

        // TodoTask.OwnerId is an opaque string, not a foreign key to User.Id - see TodoTask's own
        // remarks for why - so this comparison is a string match, not a relational join.
        var ownedTasks = await tasks
            .GetManyAsync(t => t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture), cancellationToken: ct)
            .ConfigureAwait(false);

        foreach (var task in ownedTasks)
        {
            await tasks.DeleteAsync(task, ct).ConfigureAwait(false);
        }

        await users.DeleteAsync(user, ct).ConfigureAwait(false);

        // One commit for both repositories' staged deletes - the actual point of this handler, and of
        // "one unit of work, many repositories": UnitOfWorkBehavior either commits every delete staged
        // above or, if this dispatch's outcome ends up classified as a failure, rolls all of them back
        // together (see tests/TodoTasks.Api.Tests/Persistence/DeleteUserCascadeTests for the proof).
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return default(Success);
    }
}
