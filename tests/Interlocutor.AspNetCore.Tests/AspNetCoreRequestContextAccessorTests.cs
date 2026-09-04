namespace Interlocutor.AspNetCore.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;

// Auto Generated, verify expected behavior: AspNetCoreRequestContextAccessor had no dedicated unit test file -
// only indirect coverage through AuditingBehaviorAspNetCoreIntegrationTests, which exercises it end to end
// through AuditingBehavior rather than testing its Context getter/setter contract directly.

/// <summary>Tests for <see cref="AspNetCoreRequestContextAccessor"/>.</summary>
public class AspNetCoreRequestContextAccessorTests
{
    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AspNetCoreRequestContextAccessor(null!));
    }

    [Fact]
    public void Context_WithNoCurrentHttpContext_ReturnsNull()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.Null(context);
    }

    [Fact]
    public void Context_WithCorrelationIdHeader_PopulatesCorrelationIdFromHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "correlation-123";
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("correlation-123", context.CorrelationId);
    }

    [Fact]
    public void Context_WithoutCorrelationIdHeader_FallsBackToTraceIdentifier()
    {
        // Regression test for the fallback bug: StringValues.ToString() returns "" for a missing header,
        // never null, so a naive `.ToString() ?? httpContext.TraceIdentifier` can never fall through. This
        // asserts the documented behavior - CorrelationId is the trace identifier, not "".

        // Arrange
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-456" };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("trace-456", context.CorrelationId);
    }

    [Fact]
    public void Context_WithEmptyCorrelationIdHeader_FallsBackToTraceIdentifier()
    {
        // A header present but explicitly empty should be treated the same as an absent header.

        // Arrange
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-789" };
        httpContext.Request.Headers["X-Correlation-ID"] = string.Empty;
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("trace-789", context.CorrelationId);
    }

    [Fact]
    public void Context_WithAuthenticatedUser_PopulatesUserIdFromIdentityName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "TestAuth")),
        };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("alice", context.UserId);
    }

    [Fact]
    public void Context_WithUnauthenticatedUser_LeavesUserIdNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Null(context.UserId);
    }

    [Fact]
    public void Context_WithTenantIdHeader_PopulatesTenantIdFromHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-ID"] = "tenant-789";
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("tenant-789", context.TenantId);
    }

    [Fact]
    public void Context_WithoutTenantIdHeader_FallsBackToTenantIdClaim()
    {
        // Regression test for the fallback bug: same StringValues.ToString()-never-null issue as the
        // correlation ID case above. This asserts the documented behavior - TenantId comes from the
        // tenant_id claim, not "".

        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "tenant-from-claim")], "TestAuth")),
        };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("tenant-from-claim", context.TenantId);
    }

    [Fact]
    public void Context_WithEmptyTenantIdHeader_FallsBackToTenantIdClaim()
    {
        // A header present but explicitly empty should be treated the same as an absent header.

        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "tenant-from-claim")], "TestAuth")),
        };
        httpContext.Request.Headers["X-Tenant-ID"] = string.Empty;
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal("tenant-from-claim", context.TenantId);
    }

    [Fact]
    public void Context_WithoutTenantIdHeaderOrClaim_LeavesTenantIdNull()
    {
        // Neither the header nor the claim is present, so there is no value to fall back to.

        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Null(context.TenantId);
    }

    [Fact]
    public void Context_CalledTwiceInSameRequest_ReturnsSameInstance_SoItemsStaysStable()
    {
        // Arrange - Context is built once per request and cached; otherwise a Set() followed by a later
        // TryGet() on Items would silently miss, because each .Context access would hand back a different
        // RequestContext (and therefore a different Items bag).
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var first = accessor.Context;
        var second = accessor.Context;

        // Assert
        Assert.Same(first, second);
        Assert.Same(first!.Items, second!.Items);
    }

    [Fact]
    public void Context_Setter_ThrowsNotSupportedException()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => accessor.Context = null);
    }
}
