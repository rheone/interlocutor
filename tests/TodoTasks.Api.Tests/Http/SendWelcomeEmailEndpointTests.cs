namespace TodoTasks.Api.Tests.Http;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Users.CreateUser;

/// <summary>
/// End-to-end HTTP tests for <c>POST /users/{id}/welcome-email</c> - proves the whole real, in-process-
/// hosted app (DI wiring, the <c>OverrideOutcomeCaseRegistry&lt;SendWelcomeEmailResultCaseRegistry&gt;()</c>
/// call in <c>Program.cs</c>, and the hand-rolled <c>SendWelcomeEmailResult</c>'s reflective HTTP mapping)
/// actually work together, not just that the handler logic is correct in isolation - see
/// <c>Unit/Features/Users/SendWelcomeEmail/*</c> for that.
/// </summary>
public sealed class SendWelcomeEmailEndpointTests : IClassFixture<TodoTasksApiFactory>
{
    // Minimal API responses serialize with the "Web" defaults (camelCase property names) - see
    // UsersEndpointTests' own remarks on the same options.
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TodoTasksApiFactory factory;

    public SendWelcomeEmailEndpointTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Post_AnExistingUser_TheMailerAcceptsTheAddress_ReturnsOk_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();
        var created = await CreateUserAsync(client, $"ada-{Guid.NewGuid():N}@example.com");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/users/{created.Id}/welcome-email", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_AnExistingUser_TheMailerRejectsTheAddress_ReturnsBadRequest_Test()
    {
        // Arrange - the ".invalid" TLD is what InMemoryWelcomeMailerClient treats as undeliverable; see
        // its own remarks for why that domain specifically.
        using var client = this.factory.CreateClient();
        var created = await CreateUserAsync(client, $"ada-{Guid.NewGuid():N}@example.invalid");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/users/{created.Id}/welcome-email", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken
        );

        // Assert - SendWelcomeEmailHandler adapts a mailer rejection into a Failure case, which
        // DefaultHttpOutcomeStatusMapper maps to 400 Bad Request, same as every other plain Failure in
        // this project.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownUserId_ReturnsNotFound_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/users/999999/welcome-email", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<UserDto> CreateUserAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/users", UriKind.Relative),
            new CreateUserRequest("Ada Lovelace", email),
            TestContext.Current.CancellationToken
        );
        return (await response.Content.ReadFromJsonAsync<UserDto>(WebJsonOptions, TestContext.Current.CancellationToken))!;
    }
}
