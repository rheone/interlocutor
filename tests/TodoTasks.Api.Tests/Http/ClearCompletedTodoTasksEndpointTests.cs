namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>POST /todo-tasks/clear-completed</c> - the bare-<c>ICommand</c>,
/// caller-chosen-outcome-type example.
/// </summary>
public sealed class ClearCompletedTodoTasksEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public ClearCompletedTodoTasksEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_SomeCompletedAndSomeIncompleteTasks_ClearsOnlyTheCompletedOnes_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var toComplete = await CreateTaskAsync(client, "Buy milk");
        var toLeaveAlone = await CreateTaskAsync(client, "Walk the dog");
        await client.PostAsync(
            new Uri($"/todo-tasks/{toComplete.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/clear-completed", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert - Success (no payload) maps to 204, not 200, per ToMinimalApiResult()'s default mapping.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var completedGetResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{toComplete.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, completedGetResponse.StatusCode);
        using var incompleteGetResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{toLeaveAlone.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, incompleteGetResponse.StatusCode);
    }

    [Fact]
    public async Task Post_NoAuthentication_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/clear-completed", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AuthenticateAsANewUserAsync(HttpClient client)
    {
        using var response = await client
            .PostAsJsonAsync(
                new Uri("/users", UriKind.Relative),
                new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
                TestContext.Current.CancellationToken
            )
            .ConfigureAwait(false);
        var user = await response
            .Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
    }

    private static async Task<TodoTaskDto> CreateTaskAsync(HttpClient client, string title)
    {
        using var response = await client
            .PostAsJsonAsync(
                new Uri("/todo-tasks", UriKind.Relative),
                new CreateTodoTaskRequest(title, null),
                TestContext.Current.CancellationToken
            )
            .ConfigureAwait(false);
        return (
            await response
                .Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken)
                .ConfigureAwait(false)
        )!;
    }
}
