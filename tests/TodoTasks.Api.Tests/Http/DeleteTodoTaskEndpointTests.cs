namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>DELETE /todo-tasks/{id}</c> - the guide's coarse-grained,
/// <c>IRequiresPolicy</c> authorization example.
/// </summary>
public sealed class DeleteTodoTaskEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public DeleteTodoTaskEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Delete_AuthenticatedCaller_DeletesTheTask_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(client);
        var task = await this.CreateTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.DeleteAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var getResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnauthenticatedCaller_IsRejected_BeforeTheTaskIsEverLoaded_Test()
    {
        // Arrange - authenticated caller creates the task, then a fresh, unauthenticated client tries
        // to delete it, proving the IRequiresPolicy check runs before the entity ever loads (the task
        // still exists afterward).
        using var authenticatedClient = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(authenticatedClient);
        var task = await this.CreateTaskAsync(authenticatedClient, "Buy milk");

        using var anonymousClient = this.factory.CreateClient();

        // Act
        using var response = await anonymousClient.DeleteAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        using var getResponse = await authenticatedClient.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode); // Still there - never reached the handler.
    }

    [Fact]
    public async Task Delete_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(client);

        // Act
        using var response = await client.DeleteAsync(
            new Uri("/todo-tasks/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task AuthenticateAsANewUserAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await response.Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
    }

    private async Task<TodoTaskDto> CreateTaskAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        return (await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken))!;
    }
}
