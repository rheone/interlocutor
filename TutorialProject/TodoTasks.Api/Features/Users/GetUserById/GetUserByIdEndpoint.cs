namespace TodoTasks.Api.Features.Users.GetUserById;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>GET /users/{id}</c>.</summary>
public static class GetUserByIdEndpoint
{
    /// <summary>Registers the get-user-by-id endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapGetUserByIdEndpoint(this WebApplication app)
    {
        app.MapGet(
            "/users/{id:int}",
            async (int id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(id), ct);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
