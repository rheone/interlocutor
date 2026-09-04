namespace TodoTasks.Api.Features.Users.DeleteUser;

using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Deletes a <see cref="User"/> and every <see cref="TodoTask"/> they own.</summary>
/// <param name="Id">The user to delete.</param>
public sealed record DeleteUserCommand(int Id) : ICommand<Outcome<Success, NotFound>>;
