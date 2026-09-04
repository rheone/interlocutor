namespace TodoTasks.Api.Features.Users.GetUserById;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Persistence.Abstractions;
using TodoTasks.Api.Domain;

/// <summary>
/// Handles <see cref="GetUserByIdQuery"/> - a plain read with no marker interfaces, mirroring
/// <c>documentation/beginners-guide-todo-api.md</c> Step 6's <c>GetTodoTaskByIdHandler</c> exactly: no
/// write means no transaction needs to open for this request at all.
/// </summary>
public sealed class GetUserByIdHandler(IDomainRepository<User, int> users)
    : IQueryHandler<GetUserByIdQuery, Outcome<UserDto, NotFound>>
{
    /// <inheritdoc/>
    public Task<Outcome<UserDto, NotFound>> Handle(GetUserByIdQuery query, CancellationToken ct) =>
        Task.FromResult<Outcome<UserDto, NotFound>>(
            users.GetById(query.Id) is { } user ? UserDto.FromEntity(user) : new NotFound($"User {query.Id}")
        );
}
