namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;

/// <summary>
/// End-to-end HTTP tests for <c>UsersController</c> - the MVC-controller twin of the minimal-API endpoints
/// already covered by <c>UsersEndpointTests</c>/<c>DeleteUserEndpointTests</c>. These tests exist to prove
/// the alternate <c>/api/users</c> transport genuinely dispatches through the same commands/handlers as the
/// root-level minimal API, not to re-verify handler behavior already covered there.
/// </summary>
public sealed class UsersControllerTests : IClassFixture<TodoTasksApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public UsersControllerTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task PostThenGet_ANewUser_ReturnsTheSameUserBothTimes_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var request = new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com");

        // Act
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/users", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );

        using var getResponse = await client.GetAsync(
            new Uri($"/api/users/{created!.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        var fetched = await getResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Get_UnknownUserId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/users/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AnExistingUser_DeletesThem_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );

        // Act
        using var response = await client.DeleteAsync(
            new Uri($"/api/users/{user!.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert - 200, not 204: DeleteUserCommand's success case is the generic union's TSuccess, not the
        // bare non-generic OperationOutcome ClearCompletedTodoTasksCommand uses.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var getResponse = await client.GetAsync(
            new Uri($"/api/users/{user.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
