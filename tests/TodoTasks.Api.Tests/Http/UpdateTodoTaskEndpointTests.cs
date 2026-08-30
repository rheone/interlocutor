namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>End-to-end HTTP tests for <c>PUT /todo-tasks/{id}</c> - the guide's fine-grained, resource-based authorization example.</summary>
public sealed class UpdateTodoTaskEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public UpdateTodoTaskEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Put_TheOwner_UpdatesTheirOwnTask_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var user = await CreateUserAsync(client);
        Authenticate(client, user);
        var task = await CreateTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            new UpdateTodoTaskRequest("Buy oat milk", "lactose-free"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Buy oat milk", updated!.Title);
        Assert.Equal("lactose-free", updated.Notes);
    }

    [Fact]
    public async Task Put_ADifferentAuthenticatedUser_ReturnsFailure_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var owner = await CreateUserAsync(client);
        Authenticate(client, owner);
        var task = await CreateTaskAsync(client, "Buy milk");

        var someoneElse = await CreateUserAsync(client);
        Authenticate(client, someoneElse);

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            new UpdateTodoTaskRequest("Hijacked title", null),
            TestContext.Current.CancellationToken
        );

        // Assert - ResourceCommandHandler's fixed OperationOutcome<TSuccess,NotFound,Failure> arity
        // has no dedicated "forbidden" case, so Failure maps to the default's generic failure status.
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Put_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var user = await CreateUserAsync(client);
        Authenticate(client, user);

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/todo-tasks/999999", UriKind.Relative),
            new UpdateTodoTaskRequest("Doesn't matter", null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<UserDto> CreateUserAsync(HttpClient client)
    {
        using var response = await client
            .PostAsJsonAsync(
                new Uri("/users", UriKind.Relative),
                new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
                TestContext.Current.CancellationToken
            )
            .ConfigureAwait(false);
        return (
            await response
                .Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken)
                .ConfigureAwait(false)
        )!;
    }

    private static void Authenticate(HttpClient client, UserDto user)
    {
        client.DefaultRequestHeaders.Remove(DevHeaderAuthenticationHandler.HeaderName);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user.Id.ToString());
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
