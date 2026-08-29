namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// End-to-end proof that <see cref="DevHeaderAuthenticationHandler"/> is wired up correctly - real
/// HTTP requests through the real host, not a unit test against the handler in isolation (see
/// <c>Unit/Infrastructure/DevHeaderAuthenticationHandlerTests</c> for that).
/// </summary>
public sealed class DevAuthenticationTests : IClassFixture<TodoTasksApiFactory>
{
    // See UsersEndpointTests' identical field for why this is needed: minimal APIs serialize with
    // camelCase property names, which System.Text.Json's bare defaults don't read case-insensitively.
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public DevAuthenticationTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task GetWhoami_NoHeader_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/health/whoami", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWhoami_HeaderMatchesARealUser_ReturnsThatUsersId_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com"),
            TestContext.Current.CancellationToken
        );
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, user!.Id.ToString());

        // Act
        using var response = await client.GetAsync(
            new Uri("/health/whoami", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(user.Id.ToString(), body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWhoami_HeaderDoesNotMatchAnyUser_ReturnsUnauthorized_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevHeaderAuthenticationHandler.HeaderName, "999999");

        // Act
        using var response = await client.GetAsync(
            new Uri("/health/whoami", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
