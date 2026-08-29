namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>DELETE /users/{id}</c> - proves the real, in-process-hosted app carries
/// out <c>DeleteUserHandler</c>'s multi-repository cascade correctly through actual requests, on top of
/// what <c>Unit/Features/Users/DeleteUser/DeleteUserHandlerTests</c> (mocked repositories) and
/// <c>Persistence/DeleteUserCascadeTests</c> (real database, handler called directly) already cover.
/// </summary>
public sealed class DeleteUserEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public DeleteUserEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Delete_AUserWithTasks_RemovesTheUserAndEveryOwnedTask_ButLeavesOtherUsersTasksAlone_Test()
    {
        // Arrange - one user with two tasks, and a second, unrelated user with one task.
        using var client = this.factory.CreateClient();
        var userToDelete = await this.CreateUserAsync(client);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, userToDelete.Id.ToString());
        var firstTask = await this.CreateTaskAsync(client, "Buy milk");
        var secondTask = await this.CreateTaskAsync(client, "Walk the dog");
        client.DefaultRequestHeaders.Remove(DevHeaderAuthenticationHandler.HeaderName);

        var otherUser = await this.CreateUserAsync(client);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, otherUser.Id.ToString());
        var otherUsersTask = await this.CreateTaskAsync(client, "Ship COBOL");
        client.DefaultRequestHeaders.Remove(DevHeaderAuthenticationHandler.HeaderName);

        // Act - DeleteUserEndpoint is deliberately unauthenticated (see its own remarks), so no header is needed here.
        using var deleteResponse = await client.DeleteAsync(
            new Uri($"/users/{userToDelete.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert - bare Success maps to 200 OK (see DeleteTodoTaskEndpointTests for the same mapping).
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var getDeletedUserResponse = await client.GetAsync(
            new Uri($"/users/{userToDelete.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, getDeletedUserResponse.StatusCode);

        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, otherUser.Id.ToString());
        using var getFirstTaskResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{firstTask.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        using var getSecondTaskResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{secondTask.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        using var getOtherUsersTaskResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{otherUsersTask.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, getFirstTaskResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getSecondTaskResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getOtherUsersTaskResponse.StatusCode); // The other user's task survives.
    }

    [Fact]
    public async Task Delete_UnknownUserId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.DeleteAsync(
            new Uri("/users/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<UserDto> CreateUserAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        return (await response.Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken))!;
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
