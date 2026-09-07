namespace TodoTasks.Api.Features.Tasks.CreateTodoTask;

using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using TodoTasks.Api.Infrastructure;

/// <summary>Maps <c>POST /todo-tasks</c> - the first authenticated endpoint in this project.</summary>
public static class CreateTodoTaskEndpoint
{
    /// <summary>Registers the create-task endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapCreateTodoTaskEndpoint(this WebApplication app)
    {
        app.MapPost(
                "/todo-tasks",
                async (
                    CreateTodoTaskRequest request,
                    ISender sender,
                    IRequestContextAccessor contextAccessor,
                    CancellationToken ct
                ) =>
                {
                    // A missing authenticated caller here is a bug, not an outcome the caller can
                    // trigger - RequireAuthorization() below means this endpoint is never reachable
                    // without one; see RequireUserId()'s remarks for why this throws rather than
                    // returning a Forbidden/Failure case.
                    var ownerId = contextAccessor.RequireUserId();

                    var result = await sender
                        .SendAsync(new CreateTodoTaskCommand(request.Title, request.Notes, ownerId), ct)
                        .ConfigureAwait(false);
                    return result.ToMinimalApiResult();
                }
            )
            .RequireAuthorization();

        return app;
    }
}
