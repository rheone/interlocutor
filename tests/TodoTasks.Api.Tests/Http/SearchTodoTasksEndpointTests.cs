namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>End-to-end HTTP tests for <c>GET /todo-tasks/search</c>.</summary>
public sealed class SearchTodoTasksEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public SearchTodoTasksEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Get_NoAuthentication_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/search?titleContains=milk", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ACaseInsensitiveSubstring_ReturnsOnlyThisCallersMatchingTasks_Test()
    {
        // Arrange - a distinguishing marker keeps this test's assertions independent of whatever else
        // the shared fixture's other tests have created for other owners.
        using var client = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(client);
        var marker = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest($"Buy {marker} MILK", null),
            TestContext.Current.CancellationToken
        );
        await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest($"Walk the {marker} dog", null),
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.GetAsync(
            new Uri($"/todo-tasks/search?titleContains={marker}%20milk", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<TodoTaskDto>>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        var match = Assert.Single(matches!);
        Assert.Equal($"Buy {marker} MILK", match.Title);
    }

    [Fact]
    public async Task Get_AnotherCallersMatchingTitle_IsNeverReturned_Test()
    {
        // Arrange
        var marker = Guid.NewGuid().ToString("N");
        using var otherClient = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(otherClient);
        await otherClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest($"{marker} milk", null),
            TestContext.Current.CancellationToken
        );

        using var client = this.factory.CreateClient();
        await this.AuthenticateAsANewUserAsync(client);

        // Act
        using var response = await client.GetAsync(
            new Uri($"/todo-tasks/search?titleContains={marker}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<TodoTaskDto>>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Empty(matches!);
    }

    /// <summary>Creates a real <see cref="User"/> and attaches the dev-auth header for it.</summary>
    private async Task AuthenticateAsANewUserAsync(HttpClient client)
    {
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
    }
}
