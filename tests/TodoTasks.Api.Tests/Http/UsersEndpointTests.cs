namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;

/// <summary>
/// End-to-end HTTP tests for the Users feature - proves DI wiring, FluentValidation, and outcome-to-HTTP
/// mapping all actually work together through the real, in-process-hosted app, not just that the
/// handler logic is correct in isolation (see <c>Unit/Features/Users/*</c> for that).
/// </summary>
public sealed class UsersEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    // ASP.NET Core's minimal APIs serialize responses with the "Web" defaults (camelCase property
    // names), but System.Text.Json's own bare defaults read case-sensitively - without this, reading
    // "displayName" back against UserDto's DisplayName constructor parameter fails to bind at all.
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public UsersEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task PostThenGet_ANewUser_ReturnsTheSameUserBothTimes_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var request = new CreateUserRequest("Ada Lovelace", $"ada-{Guid.NewGuid():N}@example.com");

        // Act
        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>(
            WebJsonOptions,
            TestContext.Current.CancellationToken
        );

        using var getResponse = await client.GetAsync(
            new Uri($"/users/{created!.Id}", UriKind.Relative),
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
        Assert.Equal(request.DisplayName, fetched!.DisplayName);
        Assert.Equal(request.Email, fetched.Email);
    }

    [Fact]
    public async Task Post_DuplicateEmail_ReturnsConflict_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        using var firstResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("First User", email),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode); // Sanity-check the fixture before the real assertion.

        // Act
        using var secondResponse = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Second User", email),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownUserId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/users/999999", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyDisplayName_ReturnsValidationProblem_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var request = new CreateUserRequest(string.Empty, "someone@example.com");

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken
        );

        // Assert - ToMinimalApiResult()'s default mapping for ValidationErrors, per
        // documentation/aspnetcore-integration.md's status-code table.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
