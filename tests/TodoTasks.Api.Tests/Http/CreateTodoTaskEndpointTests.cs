namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>POST /todo-tasks</c> - the first authenticated, audited,
/// eagerly-transactional endpoint in this project. Proves authorization, FluentValidation, and
/// outcome-to-HTTP mapping all work together through the real host.
/// </summary>
public sealed class CreateTodoTaskEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public CreateTodoTaskEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_AuthenticatedCaller_CreatesAndReturnsTheTask_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", "2%"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal("Buy milk", dto!.Title);
        Assert.Equal("2%", dto.Notes);
        Assert.False(dto.IsCompleted);
    }

    [Fact]
    public async Task Post_NoAuthentication_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateTitleForTheSameOwner_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        using var firstResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode); // Sanity-check the fixture before the real assertion.

        // Act
        using var secondResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", "a different note doesn't matter"),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyTitle_ReturnsValidationProblem_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(string.Empty, null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>Creates a real <see cref="User"/> and attaches the dev-auth header for it - the "create a user, then act as them" flow this project's caller identity depends on.</summary>
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
