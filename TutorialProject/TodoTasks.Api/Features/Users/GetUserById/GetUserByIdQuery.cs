namespace TodoTasks.Api.Features.Users.GetUserById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using TodoTasks.Api.Domain;

/// <summary>Looks up one <see cref="User"/> by id.</summary>
/// <param name="Id">The user's identifier.</param>
public sealed record GetUserByIdQuery(int Id) : IQuery<Outcome<UserDto, NotFound>>;
