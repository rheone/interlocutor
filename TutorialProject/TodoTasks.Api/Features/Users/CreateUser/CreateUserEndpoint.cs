namespace TodoTasks.Api.Features.Users.CreateUser;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>POST /users</c>.</summary>
public static class CreateUserEndpoint
{
    /// <summary>Registers the create-user endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapCreateUserEndpoint(this WebApplication app)
    {
        app.MapPost(
            "/users",
            async (CreateUserRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender
                    .SendAsync(new CreateUserCommand(request.DisplayName, request.Email), ct)
                    .ConfigureAwait(false);
                return result.ToMinimalApiResult();
            }
        );

        return app;
    }
}
