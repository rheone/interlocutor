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
/// <c>documentation/beginners-guide-todo-api.md</c> Step 8's "Bonus: multiple repositories, one unit
/// of work" for the fuller explanation this handler makes real: the user and every task they own are
/// deleted together or not at all, because both repositories below are built over the same scoped
/// session that one <see cref="IUnitOfWork.SaveChangesAsync"/> call commits.
/// </summary>
public sealed class DeleteUserHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteUserCommand, OperationOutcome<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<Success, NotFound>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        if (users.GetById(command.Id) is not { } user)
        {
            return new NotFound($"User {command.Id}");
        }

        // TodoTask.OwnerId is an opaque string, not a foreign key to User.Id - see TodoTask's own
        // remarks for why - so this comparison is a string match, not a relational join.
        var ownedTasks = await tasks.GetManyAsync(
            t => t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture),
            cancellationToken: ct
        );

        foreach (var task in ownedTasks)
        {
            await tasks.DeleteAsync(task, ct);
        }

        await users.DeleteAsync(user, ct);

        // One commit for both repositories' staged deletes - the actual point of this handler.
        await unitOfWork.SaveChangesAsync(ct);

        return default(Success);
    }
}
