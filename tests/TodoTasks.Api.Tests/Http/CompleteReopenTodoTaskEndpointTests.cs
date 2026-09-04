namespace TodoTasks.Api.Tests.Http;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end HTTP tests for <c>POST /todo-tasks/{id}/complete</c> and <c>POST /todo-tasks/{id}/reopen</c>,
/// including <see cref="TodoTaskCompletedNotification"/>'s effect on both of its subscribers.
/// </summary>
public sealed class CompleteReopenTodoTaskEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public CompleteReopenTodoTaskEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task PostComplete_NoAuthenticatedCaller_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/1/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostComplete_AnIncompleteTaskOwnedByTheCaller_MarksItComplete_AndIncrementsTheCompletionCounter_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var (task, owner) = await CreateAuthenticatedUserWithTaskAsync(client, "Buy milk");
        var counter = this.factory.Services.GetRequiredService<ICompletionCounter>();
        var before = counter.CountFor(owner);

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
        // PublishStrategy) - by the time the HTTP response returns, both subscribers already ran. The
        // counter is keyed by TodoTask.OwnerId - the authenticated caller's numeric user id.
        Assert.Equal(before + 1, counter.CountFor(owner));

        // A follow-up Get proves the transition actually persisted, not just the response body's own echo.
        using var getResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        var fetched = await getResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.True(fetched!.IsCompleted);
    }

    [Fact]
    public async Task PostComplete_BothNotificationSubscribersRun_Test()
    {
        // Arrange - a separately-configured host with a log-capturing provider attached, mirroring
        // CreateTodoTaskAuditingTests's own approach: the shared TodoTasksApiFactory instance other
        // tests use doesn't need this. Proves TodoTaskCompletionAuditLogHandler ran (via the captured
        // log message) and TodoTaskCompletionCounterHandler ran (via the counter) from one publish
        // dispatched through the real, ScanAssembly-discovered, DI-resolved IPublisher - the actual
        // multi-subscriber fan-out, not each subscriber tested in isolation.
        var capturedLogs = new ConcurrentBag<string>();
        // WithWebHostBuilder returns a distinct WebApplicationFactory with its own DI container (and
        // therefore its own singleton ICompletionCounter) - captured here so the counter checked below
        // is the one the request through `client` actually ran against, not this.factory's own.
        using var loggingFactory = this.factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLoggerProvider(capturedLogs)))
        );
        using var client = loggingFactory.CreateClient();
        var (task, owner) = await CreateAuthenticatedUserWithTaskAsync(client, "Buy milk");
        var counter = loggingFactory.Services.GetRequiredService<ICompletionCounter>();
        var before = counter.CountFor(owner);

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before + 1, counter.CountFor(owner));
        Assert.Contains(capturedLogs, message => message.Contains($"completed by {owner}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostComplete_AnAlreadyCompleteTask_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var (task, _) = await CreateAuthenticatedUserWithTaskAsync(client, "Buy milk");
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
    public async Task PostComplete_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_AndDoesNotCompleteIt_Test()
    {
        // Arrange
        using var ownerClient = this.factory.CreateClient();
        var (task, _) = await CreateAuthenticatedUserWithTaskAsync(ownerClient, "Buy milk");
        using var otherClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(otherClient);

        // Act
        using var response = await otherClient.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var getResponse = await ownerClient.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        var stillOwnedTask = await getResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.False(stillOwnedTask!.IsCompleted);
    }

    [Fact]
    public async Task PostComplete_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/999999/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReopen_NoAuthenticatedCaller_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/1/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostReopen_ACompletedTaskOwnedByTheCaller_MarksItIncomplete_AndPersists_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var (task, _) = await CreateAuthenticatedUserWithTaskAsync(client, "Buy milk");
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
        using var getResponse = await client.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        var fetched = await getResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.False(fetched!.IsCompleted);
    }

    [Fact]
    public async Task PostReopen_ATaskThatIsNotCompleted_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var (task, _) = await CreateAuthenticatedUserWithTaskAsync(client, "Buy milk");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostReopen_TaskExistsButIsOwnedBySomeoneElse_ReturnsForbidden_AndDoesNotReopenIt_Test()
    {
        // Arrange
        using var ownerClient = this.factory.CreateClient();
        var (task, _) = await CreateAuthenticatedUserWithTaskAsync(ownerClient, "Buy milk");
        await ownerClient.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/complete", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );
        using var otherClient = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(otherClient);

        // Act
        using var response = await otherClient.PostAsync(
            new Uri($"/todo-tasks/{task.Id}/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var getResponse = await ownerClient.GetAsync(
            new Uri($"/todo-tasks/{task.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        var stillCompletedTask = await getResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.True(stillCompletedTask!.IsCompleted);
    }

    [Fact]
    public async Task PostReopen_UnknownTaskId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        await CreateAuthenticatedUserAsync(client);

        // Act
        using var response = await client.PostAsync(
            new Uri("/todo-tasks/999999/reopen", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Registers a new <see cref="User"/> through the real <c>POST /users</c> endpoint, then attaches
    /// the dev/test authentication header to <paramref name="client"/> so subsequent requests on it are
    /// authenticated as that user - mirrors <c>TodoTasksEndpointTests</c>' own setup.
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

    /// <summary>Authenticates <paramref name="client"/> as a newly-created user and creates one task owned by them.</summary>
    /// <returns>The created task and its owner's numeric user id (the value <see cref="TodoTask.OwnerId"/> - and
    /// <see cref="ICompletionCounter"/>'s key - actually holds, not the display name used to create the user).</returns>
    private static async Task<(TodoTaskDto Task, string OwnerId)> CreateAuthenticatedUserWithTaskAsync(
        HttpClient client,
        string title
    )
    {
        var user = await CreateAuthenticatedUserAsync(client);
        using var createTaskResponse = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest(title, null),
            TestContext.Current.CancellationToken
        );
        var task = await createTaskResponse.Content.ReadFromJsonAsync<TodoTaskDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        return (task!, user.Id.ToString());
    }

    /// <summary>A minimal <see cref="ILoggerProvider"/> that appends every formatted log message to a shared bag, for assertions.</summary>
    private sealed class CapturingLoggerProvider(ConcurrentBag<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

        public void Dispose() { }

        private sealed class CapturingLogger(ConcurrentBag<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) => messages.Add(formatter(state, exception));
        }
    }
}
