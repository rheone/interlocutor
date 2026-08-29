namespace TodoTasks.Api.Tests.Http;

/// <summary>
/// Proves <see cref="TodoTasksApiFactory"/> itself works - that the real app boots end to end
/// (Autofac container, NHibernate schema export, pipeline registration) when hosted in-process for
/// tests. Feature tickets add their own endpoint tests here once an endpoint exists to call.
/// </summary>
public sealed class TodoTasksApiFactoryTests : IClassFixture<TodoTasksApiFactory>
{
    private readonly TodoTasksApiFactory factory;

    public TodoTasksApiFactoryTests(TodoTasksApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task GetHealth_AppHostedInProcess_ReturnsOk_Test()
    {
        // Arrange
        using var client = this.factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }
}
