namespace TodoTasks.Api.Tests.Persistence;

using System.Globalization;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using Interlocutor.Persistence.EntityFrameworkCore;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.DeleteUser;

/// <summary>
/// A test-only twin of <see cref="DeleteUserHandler"/> that performs the exact same cascade - load the
/// user, delete every owned task, delete the user, call <see cref="IUnitOfWork.SaveChangesAsync"/> once -
/// but then returns a forced <see cref="Conflict"/> instead of <see cref="Success"/>, simulating a
/// failure discovered only after the cascade's writes have already been flushed to the database. Exists
/// solely to prove <see cref="UnitOfWorkBehavior{TRequest,TResponse,TContext}"/>'s rollback semantics
/// undo a flushed-but-uncommitted cascade; it is never registered with the application's pipeline and
/// models no real feature.
/// </summary>
internal sealed class DeleteUserThenFailHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork
)
{
    /// <summary>Runs the cascade, then forces a <see cref="Conflict"/> failure after it has flushed.</summary>
    /// <param name="command">The user to delete.</param>
    /// <param name="ct">A token to observe while the cascade runs.</param>
    /// <returns>
    /// <see cref="NotFound"/> if the user doesn't exist (the cascade never starts); otherwise always a
    /// forced <see cref="Conflict"/>, returned only after every delete has been flushed.
    /// </returns>
    public async Task<Outcome<Success, NotFound, Conflict>> HandleAsync(DeleteUserThenFailCommand command, CancellationToken ct)
    {
        if (users.GetById(command.Id) is not { } user)
        {
            return new NotFound($"User {command.Id}");
        }

        var ownedTasks = await tasks
            .GetManyAsync(t => t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture), cancellationToken: ct)
            .ConfigureAwait(false);

        foreach (var task in ownedTasks)
        {
            await tasks.DeleteAsync(task, ct).ConfigureAwait(false);
        }

        await users.DeleteAsync(user, ct).ConfigureAwait(false);

        // Everything up to here is real, unmodified cascade behavior - this flushes the deletes above to
        // the database within the transaction the commit-gate behavior will decide whether to commit or
        // roll back.
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // The forced failure, returned only after the flush above: the pipeline's commit-gate behavior
        // sees a failure-classified outcome and rolls back a transaction that has already had real DELETE
        // statements executed against it.
        return new Conflict(Message: "Test-only forced failure after the cascade's flush.", Code: "forced_failure");
    }
}
