namespace TodoTasks.Api.Features.Users.CreateUser;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>Handles <see cref="CreateUserCommand"/>.</summary>
public sealed class CreateUserHandler(IDomainRepository<User, int> users, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateUserCommand, OperationOutcome<UserDto, ValidationErrors, Conflict>>
{
    /// <inheritdoc/>
    public async Task<OperationOutcome<UserDto, ValidationErrors, Conflict>> Handle(
        CreateUserCommand command,
        CancellationToken ct
    )
    {
        // Shares CreateUserCommand's ITransactionalRequest-eager transaction with the insert below - see
        // that command's own remarks for why this specific ordering needs one.
        var emailAlreadyTaken =
            (await users.GetManyAsync(u => u.Email == command.Email, cancellationToken: ct).ConfigureAwait(false)).Count > 0;

        if (emailAlreadyTaken)
        {
            return new Conflict(Message: $"A user with email '{command.Email}' already exists.", Code: "duplicate_email");
        }

        var user = new User(command.DisplayName, command.Email);
        await users.InsertAsync(user, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false); // UnitOfWorkBehavior commits from here on success.

        return UserDto.FromEntity(user);
    }
}
