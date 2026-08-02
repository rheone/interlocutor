namespace Mediance.AspNetCore;

using Mediance.Abstractions.Core;
using Microsoft.AspNetCore.Http;

/// <summary>
/// ASP.NET Core <see cref="IRequestContextAccessor"/> implementation that populates request context from the authenticated caller.
/// </summary>
/// <remarks>
/// <para>
/// This implementation extracts correlation ID, user identity, and tenant information from the current HTTP context.
/// It populates <see cref="RequestContext"/> properties from:
/// <list type="bullet">
///   <item><description>CorrelationId - From X-Correlation-ID header, or generated if absent.</description></item>
///   <item><description>User - From HttpContext.User (authenticated identity).</description></item>
///   <item><description>TenantId - From X-Tenant-ID header or claims if available.</description></item>
/// </list>
/// </para>
/// <para>
/// Register this with <c>AddScoped</c> lifetime in the dependency injection container. This implementation
/// automatically populates the request context during HTTP request processing, before handlers execute.
/// </para>
/// </remarks>
public sealed class AspNetCoreRequestContextAccessor(IHttpContextAccessor httpContextAccessor) : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor
        ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc/>
    public RequestContext? Context
    {
        get
        {
            var httpContext = this._httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            var correlationId = httpContext.Request.Headers["X-Correlation-ID"].ToString()
                ?? httpContext.TraceIdentifier;

            var userId = httpContext.User?.Identity?.Name;

            var tenantId = httpContext.Request.Headers["X-Tenant-ID"].ToString()
                ?? httpContext.User?.FindFirst("tenant_id")?.Value;

            return new RequestContext
            {
                CorrelationId = correlationId,
                UserId = userId,
                TenantId = tenantId,
            };
        }

        set => throw new NotSupportedException("Cannot set request context on web-backed accessor.");
    }
}
