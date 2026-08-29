namespace TodoTasks.Api.Tests.Unit.Infrastructure;

using System.Text.Encodings.Web;
using Interlocutor.Persistence.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Infrastructure;

/// <summary>
/// Unit tests for <see cref="DevHeaderAuthenticationHandler"/>'s three outcomes, driving it exactly
/// the way ASP.NET Core's authentication middleware does - <c>InitializeAsync</c> then
/// <c>AuthenticateAsync</c> - against a fake <see cref="HttpContext"/> and a mocked
/// <see cref="IDomainRepository{TEntity,TKey}"/>, no real HTTP involved.
/// </summary>
public sealed class DevHeaderAuthenticationHandlerTests
{
    private readonly IDomainRepository<User, int> users = Substitute.For<IDomainRepository<User, int>>();

    [Fact]
    public async Task AuthenticateAsync_NoHeader_IsNotAuthenticated_Test()
    {
        // Arrange
        var handler = await this.CreateInitializedHandlerAsync(new DefaultHttpContext());

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        this.users.DidNotReceive().GetById(Arg.Any<int>());
    }

    [Fact]
    public async Task AuthenticateAsync_HeaderDoesNotMatchAnyUser_IsNotAuthenticated_Test()
    {
        // Arrange
        this.users.GetById(Arg.Any<int>()).Returns((User?)null);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[DevHeaderAuthenticationHandler.HeaderName] = "999";
        var handler = await this.CreateInitializedHandlerAsync(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticateAsync_HeaderIsNotAnInteger_IsNotAuthenticated_Test()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[DevHeaderAuthenticationHandler.HeaderName] = "not-a-number";
        var handler = await this.CreateInitializedHandlerAsync(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        this.users.DidNotReceive().GetById(Arg.Any<int>());
    }

    [Fact]
    public async Task AuthenticateAsync_HeaderMatchesARealUser_IsAuthenticated_WithUserIdAsTheIdentityName_Test()
    {
        // Arrange
        var user = new User("Ada Lovelace", "ada@example.com");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, 42); // Id has a protected setter - only NHibernate sets it normally.
        this.users.GetById(42).Returns(user);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[DevHeaderAuthenticationHandler.HeaderName] = "42";
        var handler = await this.CreateInitializedHandlerAsync(httpContext);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert - AspNetCoreRequestContextAccessor reads RequestContext.UserId from
        // HttpContext.User.Identity.Name specifically (verified against its shipped source) - this is
        // the exact property this project's endpoints depend on for the caller's identity.
        Assert.True(result.Succeeded);
        Assert.Equal("42", result.Principal?.Identity?.Name);
    }

    private async Task<DevHeaderAuthenticationHandler> CreateInitializedHandlerAsync(HttpContext httpContext)
    {
        var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
        var handler = new DevHeaderAuthenticationHandler(options, NullLoggerFactory.Instance, UrlEncoder.Default, this.users);
        var scheme = new AuthenticationScheme(
            DevHeaderAuthenticationHandler.SchemeName,
            displayName: null,
            typeof(DevHeaderAuthenticationHandler)
        );
        await handler.InitializeAsync(scheme, httpContext);
        return handler;
    }
}
