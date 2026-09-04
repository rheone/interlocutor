namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for the TodoTasks Create feature - proves DI wiring, authentication,
/// FluentValidation, and outcome-to-HTTP mapping all actually work together through the real,
/// in-process-hosted app, not just that the handler logic is correct in isolation (see
/// <c>Unit/Features/Tasks/CreateTodoTask/*</c> for that).
/// </summary>
public sealed class TodoTasksEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    // ASP.NET Core's minimal APIs serialize responses with the "Web" defaults (camelCase property
    // names), but System.Text.Json's own bare defaults read case-sensitively - without this, reading
    // "isCompleted" back against TodoTaskDto's IsCompleted constructor parameter fails to bind at all.
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public TodoTasksEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_NoAuthenticatedCaller_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var request = new CreateTodoTaskRequest("Buy milk", null);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );

        // Assert - RequireAuthorization() on the endpoint rejects this before CreateTodoTaskHandler ever
        // runs; see Unit/Infrastructure for the unit-level equivalent of "no caller" behavior.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_AuthenticatedCaller_CreatesATaskOwnedByThatCaller_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var caller = await CreateAuthenticatedUserAsync(client);
        var request = new CreateTodoTaskRequest("Buy milk", "2%, please");

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );

        // Assert - CreateTodoTaskRequest has no OwnerId member at all (see its own remarks), so there is
        // no request-body value that could have supplied it; the created task's ownership can only have
        // come from the authenticated caller read off IRequestContextAccessor inside the endpoint.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TodoTaskDto>(WebJsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal("Buy milk", dto.Title);
        Assert.Equal("2%, please", dto.Notes);
        Assert.False(dto.IsCompleted);

        _ = caller; // The caller's identity is exercised indirectly below (same-owner conflict, cross-owner non-conflict).
    }

    [Fact]
    public async Task Post_DuplicateTitleForTheSameAuthenticatedCaller_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);
        var title = $"Duplicate-{Guid.NewGuid():N}";
        using var firstResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode); // Sanity-check the fixture before the real assertion.

        // Act
        using var secondResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Post_SameTitleForTwoDifferentAuthenticatedCallers_BothSucceed_Test()
    {
        // Arrange - proves the duplicate-title check (and, by extension, the ownership it's scoped to)
        // tracks the actual authenticated caller per request rather than some fixed value: if OwnerId
        // were not genuinely sourced per-caller, the second create below would incorrectly conflict with
        // the first.
        using var firstClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(firstClient);
        using var secondClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(secondClient);
        var title = $"Shared-{Guid.NewGuid():N}";

        // Act
        using var firstResponse = await firstClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        using var secondResponse = await secondClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyTitle_ReturnsValidationProblem_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);
        var request = new CreateTodoTaskRequest(string.Empty, null);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );

        // Assert - ToMinimalApiResult()'s default mapping for ValidationErrors, per
        // documentation/aspnetcore-integration.md's status-code table.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_NoAuthenticatedCaller_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/1", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_TaskOwnedByTheAuthenticatedCaller_ReturnsIt_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", "2%, please"),
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
    public async Task Get_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_TaskExistsButIsOwnedBySomeoneElse_ReturnsNotFound_Test()
    {
        // Arrange - proves a non-owner gets exactly the same response as an unknown id (Get_UnknownTaskId_
        // ReturnsNotFound_Test above): existence is never leaked to a caller who isn't the owner.
        using var ownerClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(ownerClient);
        using var createResponse = await ownerClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );
        var created = await createResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        using var otherClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(otherClient);

        // Act
        using var response = await otherClient.GetAsync(
            new Uri($"/todo-tasks/{created!.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_NoAuthenticatedCaller_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_TwoAuthenticatedCallersEachHaveTasks_OnlyReturnsTheCallersOwnTasks_Test()
    {
        // Arrange
        using var firstClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(firstClient);
        using var secondClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(secondClient);
        var firstTitle = $"First owner's task-{Guid.NewGuid():N}";
        var secondTitle = $"Second owner's task-{Guid.NewGuid():N}";
        using var firstCreate = await firstClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(firstTitle, null),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, firstCreate.StatusCode); // Sanity-check the fixture before the real assertion.
        using var secondCreate = await secondClient.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(secondTitle, null),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, secondCreate.StatusCode);

        // Act
        using var response = await firstClient.GetAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TodoTaskDto>>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(tasks);
        Assert.Contains(tasks, t => t.Title == firstTitle);
        Assert.DoesNotContain(tasks, t => t.Title == secondTitle);
    }

    /// <summary>
    /// Registers a new <see cref="User"/> through the real <c>POST /users</c> endpoint, then attaches
    /// the dev/test authentication header to <paramref name="client"/> so subsequent requests on it are
    /// authenticated as that user - mirrors <c>DevAuthenticationTests</c>' own setup.
    /// </summary>
    private static async Task<UserDto> CreateAuthenticatedUserAsync(HttpClient client)
    {
        using var createUserResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Caller", $"caller-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());
        return user;
    }
}
