namespace TodoTasks.Api.Tests.Http;

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Proves the library's own <c>LoggingAuditSink</c> (try-added as <c>IAuditSink</c>'s default - see
/// <c>Program.cs</c>'s own remarks for why this project doesn't write a custom one) actually records
/// <see cref="CreateTodoTaskCommand.ActionName"/>, not just that the endpoint returns the right HTTP
/// status. Uses a real, separately-configured host (<c>WebApplicationFactory{TEntryPoint}.WithWebHostBuilder</c>)
/// with a log-capturing provider attached, since the shared <see cref="TodoTasksApiFactory"/> instance
/// other tests use doesn't need this.
/// </summary>
public sealed class CreateTodoTaskAuditingTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public CreateTodoTaskAuditingTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_ACreatedTask_IsRecordedByTheAuditSink_Test()
    {
        // Arrange
        var capturedLogs = new ConcurrentBag<string>();
        using var client = this
            .factory.WithWebHostBuilder(builder =>
                builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLoggerProvider(capturedLogs)))
            )
            .CreateClient();

        using var createUserResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createUserResponse.Content.ReadFromJsonAsync<TodoTasks.Api.Domain.UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/todo-tasks", UriKind.Relative),
            new CreateTodoTaskRequest("Buy milk", null),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains(capturedLogs, message => message.Contains("create_todo_task", StringComparison.Ordinal));
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
