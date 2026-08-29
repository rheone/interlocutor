namespace TodoTasks.Api.Features.Users;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Mvc;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Features.Users.DeleteUser;
using TodoTasks.Api.Features.Users.GetUserById;

/// <summary>
/// The MVC-controller twin of this domain's minimal-API surface (<c>Features/Users/*/Map*Endpoint.cs</c>,
/// mapped at the root, e.g. <c>/users</c>) - the same commands/queries/handlers, dispatched the same way,
/// exposed under <c>/api/users</c> instead. See <c>Features/Tasks/TodoTasksController</c>'s own remarks for
/// why both transports run through the identical registered pipeline with no behavior-level changes.
/// </summary>
/// <remarks>
/// Constructor injection of <see cref="ISender"/> is the same inversion-of-control shape every handler and
/// minimal-API endpoint in this project already uses - this controller depends only on Interlocutor's own
/// abstraction, never on a concrete handler, repository, or container.
/// </remarks>
[ApiController]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>Handles <c>POST /api/users</c> - the controller twin of <c>CreateUserEndpoint</c>.</summary>
    /// <param name="request">The user to create.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the created user, or the mapped failure (e.g. 409 on a duplicate email).</returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateUserCommand(request.DisplayName, request.Email), ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles <c>GET /api/users/{id}</c> - the controller twin of <c>GetUserByIdEndpoint</c>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="ResultExtensions.ToActionResult{TSuccess,TFailure}"/>, the convenience path -
    /// deliberate contrast with <c>TodoTasksController.GetById</c>, which handles the identically-shaped
    /// <c>OperationOutcome&lt;TodoTaskDto, NotFound&gt;</c> union via a manual pattern-match instead. Read
    /// the two together to see both styles applied to the same union shape: this one delegates the
    /// success/<see cref="NotFound"/> mapping once, to code shared by every other action in both
    /// controllers; the other spells the same two cases out by hand.
    /// </remarks>
    /// <param name="id">The user's identifier.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>200 with the user on success, or 404 if no user with that id exists.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles <c>DELETE /api/users/{id}</c> - the controller twin of <c>DeleteUserEndpoint</c>.
    /// Unauthenticated, matching the rest of the Users feature - user management itself isn't gated behind
    /// caller identity in this proof of concept.
    /// </summary>
    /// <param name="id">The user to delete.</param>
    /// <param name="ct">A token to observe while dispatching.</param>
    /// <returns>
    /// 200 with an empty <see cref="Success"/> body on success (<see cref="DeleteUserCommand"/> returns
    /// <c>OperationOutcome&lt;Success, NotFound&gt;</c>, whose success case <c>ToActionResult()</c> maps
    /// the same way as any other generic union success case), or 404 if the user doesn't exist. Every
    /// <see cref="TodoTask"/> the user owns is deleted along with them, in one unit of work - see
    /// <c>DeleteUserHandler</c>'s own remarks for the multi-repository cascade.
    /// </returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUserCommand(id), ct);
        return result.ToActionResult();
    }
}
