namespace Interlocutor.AspNetCore.Tests;

using System.Security.Claims;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using NSubstitute;

/// <summary>
/// Proves that <see cref="AuditingBehavior{TRequest,TResponse}"/> resolves <see cref="AuditEntry.UserId"/>
/// through the real <see cref="AspNetCoreRequestContextAccessor"/> - not a hand-rolled test double for
/// <see cref="Interlocutor.Abstractions.Core.IRequestContextAccessor"/> - end to end through a
/// <see cref="AuditingBehavior{TRequest,TResponse}.HandleAsync"/> call.
/// </summary>
public class AuditingBehaviorAspNetCoreIntegrationTests
{
    [Fact]
    public async Task Handle_WithAuthenticatedHttpContext_PopulatesUserIdFromRealAccessor()
    {
        // Arrange - a real ASP.NET Core HttpContext with an authenticated identity, wired through the
        // shipped AspNetCoreRequestContextAccessor rather than a mock of IRequestContextAccessor.
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "TestAuth"));

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var contextAccessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink, contextAccessor: contextAccessor);
        var request = new AuditableQuery("test");

        // Act
        await behavior.HandleAsync(request, () => Task.FromResult("response"), CancellationToken.None);

        // Assert
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.UserId == "alice"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithUnauthenticatedHttpContext_LeavesUserIdNull()
    {
        // Arrange - a real HttpContext with no authenticated identity attached (the anonymous case).
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var contextAccessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        var sink = Substitute.For<IAuditSink>();
        var behavior = new AuditingBehavior<AuditableQuery, string>(sink, contextAccessor: contextAccessor);
        var request = new AuditableQuery("test");

        // Act
        var result = await behavior.HandleAsync(request, () => Task.FromResult("response"), CancellationToken.None);

        // Assert - no exception, auditing still succeeds, UserId stays null.
        Assert.Equal("response", result);
        await sink.Received(1).RecordAsync(Arg.Is<AuditEntry>(e => e.UserId == null), CancellationToken.None);
    }

    public record AuditableQuery(string Name) : IQuery<Outcome<string, Failure>>, IAuditableRequest
    {
        public string ActionName => "Query.Read";
    }
}
