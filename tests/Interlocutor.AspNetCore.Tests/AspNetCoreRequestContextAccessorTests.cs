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
    public void Context_WithoutCorrelationIdHeader_DoesNotFallBackToTraceIdentifier()
    {
        // Auto Generated, verify expected behavior: the doc comment on AspNetCoreRequestContextAccessor.Context
        // claims the correlation ID falls back to TraceIdentifier when the header is absent, via
        // `httpContext.Request.Headers["X-Correlation-ID"].ToString() ?? httpContext.TraceIdentifier`. But
        // StringValues.ToString() returns "" for a missing header, never null, so the ?? fallback can never
        // fire - the resulting CorrelationId is "" rather than the trace identifier. This asserts the actual
        // current behavior; a human should confirm whether the empty-string result or the documented
        // fallback-to-TraceIdentifier behavior is intended.

        // Arrange
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-456" };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal(string.Empty, context.CorrelationId);
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
    public void Context_WithoutTenantIdHeader_DoesNotFallBackToTenantIdClaim()
    {
        // Auto Generated, verify expected behavior: same StringValues.ToString()-never-null issue as the
        // correlation ID case above - `httpContext.Request.Headers["X-Tenant-ID"].ToString() ?? ...FindFirst
        // ("tenant_id")?.Value` never falls back to the claim, because ToString() on a missing header returns
        // "" rather than null. This asserts the actual current behavior; a human should confirm whether the
        // empty-string result or the documented fallback-to-claim behavior is intended.

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
        Assert.Equal(string.Empty, context.TenantId);
    }

    [Fact]
    public void Context_WithoutTenantIdHeaderOrClaim_LeavesTenantIdEmpty()
    {
        // Auto Generated, verify expected behavior: see the note on
        // Context_WithoutTenantIdHeader_DoesNotFallBackToTenantIdClaim - the missing-header case always
        // produces "", never null, regardless of whether a tenant_id claim is present.

        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var accessor = new AspNetCoreRequestContextAccessor(httpContextAccessor);

        // Act
        var context = accessor.Context;

        // Assert
        Assert.NotNull(context);
        Assert.Equal(string.Empty, context.TenantId);
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
