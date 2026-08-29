namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>End-to-end HTTP tests for <c>POST /todo-tasks/{id}/complete</c> and <c>POST /todo-tasks/{id}/reopen</c>, including the notification's effect on <see cref="ICompletionCounter"/>.</summary>
public sealed class CompleteReopenTodoTaskEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public CompleteReopenTodoTaskEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task PostComplete_AnIncompleteTask_MarksItComplete_AndIncrementsTheCompletionCounter_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var (task, ownerUserId) = await this.CreateTaskAndReturnItsOwnerIdAsync(
            client,
            $"owner-{Guid.NewGuid():N}",
            "Buy milk"
        );
        var counter = this.factory.Services.GetRequiredService<ICompletionCounter>();
        var before = counter.CountFor(ownerUserId);

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        Assert.True(dto!.IsCompleted);
        // Publish is fire-and-forget from the caller's perspective, but sequential (the default
        // PublishStrategy) - by the time the HTTP response returns, both subscribers already ran.
        // The counter is keyed by TodoTask.OwnerId - the authenticated caller's numeric user id, not
        // the display name used to create that user.
        Assert.Equal(before + 1, counter.CountFor(ownerUserId));
    }

    [Fact]
    public async Task PostComplete_AnAlreadyCompleteTask_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var task = await this.CreateTaskAsync(client, $"owner-{Guid.NewGuid():N}", "Buy milk");
        await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostReopen_ACompletedTask_MarksItIncomplete_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var task = await this.CreateTaskAsync(client, $"owner-{Guid.NewGuid():N}", "Buy milk");
        await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        Assert.False(dto!.IsCompleted);
    }

    [Fact]
    public async Task PostReopen_ATaskThatIsNotCompleted_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var task = await this.CreateTaskAsync(client, $"owner-{Guid.NewGuid():N}", "Buy milk");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<TodoTaskDto> CreateTaskAsync(HttpClient client, string displayName, string title) =>
        (await this.CreateTaskAndReturnItsOwnerIdAsync(client, displayName, title)).Task;

    /// <summary>Creates a user, authenticates as them, and creates a task - returning both the task and the owner's actual numeric user id (the counter's real key, not the display name).</summary>
    private async Task<(TodoTaskDto Task, string OwnerUserId)> CreateTaskAndReturnItsOwnerIdAsync(
        HttpClient client,
        string displayName,
        string title
    )
    {
        client.DefaultRequestHeaders.Remove(DevHeaderAuthenticationHandler.HeaderName);
        using var createUserResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest(displayName, $"{displayName}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        var ownerUserId = user!.Id.ToString();
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, ownerUserId);

        using var createTaskResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        var task = (
            await createTaskResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
                WebJsonOptions,
                TestContext.Current.CancellationToken
            )
        )!;
        return (task, ownerUserId);
    }
}
