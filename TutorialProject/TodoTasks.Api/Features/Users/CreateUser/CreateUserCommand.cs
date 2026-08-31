namespace TodoTasks.Api.Features.Users.CreateUser;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>
/// Creates a new <see cref="User"/>.
/// </summary>
/// <param name="DisplayName">The new user's display name.</param>
/// <param name="Email">
/// The new user's email address - must not already belong to another user; see
/// <see cref="CreateUserHandler"/>'s duplicate-email check.
/// </param>
/// <remarks>
/// <see cref="ITransactionalRequest"/> is here for the same reason
/// <c>documentation/beginners-guide-todo-api.md</c> Step 5's <c>CreateTodoTaskCommand</c> carries it: the
/// duplicate-email read in <see cref="CreateUserHandler"/> and the insert that follows it need to share
/// one transaction, or two concurrent requests for the same email could both pass the check before either
/// commits.
/// </remarks>
public sealed record CreateUserCommand(string DisplayName, string Email)
    : ICommand<Outcome<UserDto, ValidationErrors, Conflict>>,
        ITransactionalRequest;
