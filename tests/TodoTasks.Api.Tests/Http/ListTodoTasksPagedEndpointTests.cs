namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>End-to-end HTTP tests for <c>GET /todo-tasks/paged</c>.</summary>
public sealed class ListTodoTasksPagedEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public ListTodoTasksPagedEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Get_NoAuthentication_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/paged?page=1&pageSize=10", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ALastPartialPage_ReturnsOnlyTheRemainingItems_WithTheRealTotalCount_Test()
    {
        // Arrange - this caller's own five tasks, requested two at a time: pages 1 and 2 are full,
        // page 3 is the boundary case - a single, partial, last page.
        using var client = this.factory.CreateClient();
        await AuthenticateAsANewUserAsync(client);
        var marker = Guid.NewGuid().ToString("N");
        for (var i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync(
                new Uri("/todo-tasks", UriKind.Relative),
                new CreateTodoTaskRequest($"{marker}-{i}", null),
                TestContext.Current.CancellationToken
            );
        }

        // Act
        using var response = await client.GetAsync(
            new Uri("/todo-tasks/paged?page=3&pageSize=2", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedTodoTasksDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        Assert.Single(page!.Items);
        Assert.Equal(3, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalCount);
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
