namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>End-to-end HTTP tests for <c>GET /todo-tasks/{id}</c> and <c>GET /todo-tasks</c>.</summary>
public sealed class TodoTasksReadEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public TodoTasksReadEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Get_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ATaskJustCreated_ReturnsIt_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );
        var created = await createResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.GetAsync(
            new Uri($"/todo-tasks/{created!.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task GetList_WithOnlyIncompleteFalse_ReturnsAllOfThisTestsOwnCreatedTasks_Test()
    {
        // Arrange - a distinguishing title lets this test find its own tasks among whatever else the
        // shared fixture's other tests created, since /todo-tasks isn't scoped to a caller in the
        // guide's own running example.
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var marker = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest($"{marker}-one", null),
            TestContext.Current.CancellationToken
        );
        await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest($"{marker}-two", null),
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks?onlyIncomplete=false", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var all = await response.Content.ReadFromJsonAsync<List<TodoTaskDto>>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        var thisTests = all!.Where(t => t.Title.StartsWith(marker, StringComparison.Ordinal)).ToList();
        Assert.Equal(2, thisTests.Count);
    }

    /// <summary>Creates a real <see cref="User"/> and attaches the dev-auth header for it.</summary>
    private static async Task AuthenticateAsANewUserAsync(HttpClient client)
    {
        using var createResponse = await client
            .PostAsJsonAsync(
                new Uri("/users", UriKind.Relative),
                new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
                TestContext.Current.CancellationToken
            )
            .ConfigureAwait(false);
        var user = await createResponse
            .Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
    }
}
