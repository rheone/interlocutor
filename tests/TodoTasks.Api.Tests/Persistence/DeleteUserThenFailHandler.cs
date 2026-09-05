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
/// but then explicitly cancels via <see cref="IUnitOfWork.CancelAsync"/> and returns a forced
/// <see cref="Conflict"/> instead of <see cref="Success"/>, simulating a failure discovered only after
/// the cascade's writes have already been flushed to the database. No case type - <see cref="Conflict"/>
/// included - decides commit vs. rollback on its own; the explicit <see cref="IUnitOfWork.CancelAsync"/>
/// call is what forces the rollback here. Exists solely to prove <c>UnitOfWorkBehavior{TRequest,TResponse,TContext}</c>
/// commits a flushed cascade unless told otherwise; it is never registered with the application's
/// pipeline and models no real feature.
/// </summary>
internal sealed class DeleteUserThenFailHandler(
    IDomainRepository<User, int> users,
    IDomainRepository<TodoTask, int> tasks,
    IUnitOfWork unitOfWork,
    bool cancelBeforeReturning = true
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
        // the database within the transaction the commit-gate behavior will otherwise commit.
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // The forced failure, discovered only after the flush above: since no case type decides
        // commit-vs-rollback any more, an explicit Cancel is what rolls back a transaction that has
        // already had real DELETE statements executed against it - Conflict alone would not, which is
        // exactly what cancelBeforeReturning: false proves (see DeleteUserCascadeTests' companion test).
        if (cancelBeforeReturning)
        {
            await unitOfWork.CancelAsync(ct).ConfigureAwait(false);
        }

        return new Conflict(Message: "Test-only forced failure after the cascade's flush.", Code: "forced_failure");
    }
}
