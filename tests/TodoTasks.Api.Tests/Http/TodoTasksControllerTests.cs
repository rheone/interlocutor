namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>TodoTasksController</c> - the MVC-controller twin of the minimal-API
/// endpoints already covered by <c>TodoTasksReadEndpointTests</c>, <c>UpdateTodoTaskEndpointTests</c>, etc.
/// These tests exist to prove the alternate <c>/api/todo-tasks</c> transport genuinely dispatches through
/// the same commands/handlers/pipeline as the root-level minimal API, not to re-verify handler behavior
/// already covered there.
/// </summary>
public sealed class TodoTasksControllerTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public TodoTasksControllerTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_ANewTask_CreatesIt_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );

        // Assert - ToActionResult()'s success case is always 200, not 201, same as ToMinimalApiResult().
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Buy milk", created!.Title);
    }

    [Fact]
    public async Task GetById_ATaskJustCreated_ReturnsIt_Test()
    {
        // Arrange - exercises GetById's manual pattern-match over the success case.
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var task = await CreateTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(task, fetched);
    }

    [Fact]
    public async Task GetById_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange - exercises GetById's manual pattern-match over the NotFound case.
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/todo-tasks/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetList_WithOnlyIncompleteFalse_ReturnsThisTestsOwnCreatedTasks_Test()
    {
        // Arrange - a distinguishing title lets this test find its own tasks among the shared fixture's
        // other tests' tasks, since /api/todo-tasks isn't scoped to a caller.
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var marker = Guid.NewGuid().ToString("N");
        await CreateTaskAsync(client, $"{marker}-one");
        await CreateTaskAsync(client, $"{marker}-two");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/todo-tasks?onlyIncomplete=false", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var all = await response.Content.ReadFromJsonAsync<List<TodoTaskDto>>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(2, all!.Count(t => t.Title.StartsWith(marker, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Put_TheOwner_UpdatesTheirOwnTask_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var task = await CreateTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/todo-tasks/{task.Id}", UriKind.Relative),
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
    }

    [Fact]
    public async Task PostComplete_ThenPostReopen_RoundTripsCompletionState_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var task = await CreateTaskAsync(client, "Buy milk");

        // Act
        using var completeResponse = await client.PostAsync(
            new Uri($"/api/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );
        using var reopenResponse = await client.PostAsync(
            new Uri($"/api/todo-tasks/{task.Id}/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.True(completed!.IsCompleted);

        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.False(reopened!.IsCompleted);
    }

    [Fact]
    public async Task Delete_AuthenticatedCaller_DeletesTheTask_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var task = await CreateTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.DeleteAsync(
            new Uri($"/api/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert - 200, not 204: DeleteTodoTaskCommand's success case is the generic union's TSuccess, not
        // the bare non-generic OperationOutcome ClearCompleted uses below.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var getResponse = await client.GetAsync(
            new Uri($"/api/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostClearCompleted_SomeCompletedAndSomeIncompleteTasks_ClearsOnlyTheCompletedOnes_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var toComplete = await CreateTaskAsync(client, "Buy milk");
        var toLeaveAlone = await CreateTaskAsync(client, "Walk the dog");
        await client.PostAsync(
            new Uri($"/api/todo-tasks/{toComplete.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/todo-tasks/clear-completed", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert - bare ICommand dispatched as sender.Send<OperationOutcome>(...), whose Success case
        // maps to 204, not 200 - the same non-generic overload the minimal-API endpoint uses.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var completedGetResponse = await client.GetAsync(
            new Uri($"/api/todo-tasks/{toComplete.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, completedGetResponse.StatusCode);
        using var incompleteGetResponse = await client.GetAsync(
            new Uri($"/api/todo-tasks/{toLeaveAlone.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, incompleteGetResponse.StatusCode);
    }

    private static async Task AuthenticateAsANewUserAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await response.Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
    }

    private static async Task<TodoTaskDto> CreateTaskAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        return (await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken))!;
    }
}
