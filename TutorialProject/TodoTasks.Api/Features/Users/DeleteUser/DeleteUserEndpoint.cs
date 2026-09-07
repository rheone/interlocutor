namespace TodoTasks.Api.Features.Users.DeleteUser;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>
/// Maps <c>DELETE /users/{id}</c>. Unauthenticated, matching the rest of the Users feature
/// (<c>Features/Users/CreateUser</c>, <c>Features/Users/GetUserById</c>) - user management itself isn't
/// gated behind caller identity in this proof of concept.
/// </summary>
public static class DeleteUserEndpoint
{
    /// <summary>Registers the delete-user endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapDeleteUserEndpoint(this WebApplication app)
    {
        app.MapDelete(
            "/users/{id:int}",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.SendAsync(new DeleteUserCommand(id), ct).ConfigureAwait(false);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
