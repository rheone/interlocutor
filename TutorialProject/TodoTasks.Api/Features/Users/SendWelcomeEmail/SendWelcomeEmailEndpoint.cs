namespace TodoTasks.Api.Features.Users.SendWelcomeEmail;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.AspNetCore;

/// <summary>Maps <c>POST /users/{id}/welcome-email</c>.</summary>
public static class SendWelcomeEmailEndpoint
{
    /// <summary>Registers the send-welcome-email endpoint on <paramref name="app"/>.</summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns><paramref name="app"/>, for chaining alongside this project's other <c>Map*Endpoint</c> calls.</returns>
    public static WebApplication MapSendWelcomeEmailEndpoint(this WebApplication app)
    {
        app.MapPost(
            "/users/{id:int}/welcome-email",
            async (int id, ISender sender, IOutcomeCaseRegistry caseRegistry, CancellationToken ct) =>
            {
                var result = await sender.SendAsync(new SendWelcomeEmailCommand(id), ct).ConfigureAwait(false);

                // SendWelcomeEmailResult is a hand-rolled IUnion, not one of the compiler-generated
                // Outcome<...> arities ToMinimalApiResult() has a dedicated overload for, so this calls the
                // reflective catch-all overload instead, passing the IOutcomeCaseRegistry Program.cs
                // resolves via its OverrideOutcomeCaseRegistry<SendWelcomeEmailResultCaseRegistry>() call.
                return result.ToMinimalApiResult(caseRegistry);
            }
        );

        return app;
    }
}
