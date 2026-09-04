namespace Interlocutor.AspNetCore;

using System.Threading;
using Interlocutor.Abstractions.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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
/// <para>
/// The <see cref="RequestContext"/> returned by <see cref="Context"/> - and therefore its
/// <see cref="RequestContext.Items"/> bag - is built once per request and cached for the life of this
/// instance (itself scoped per request), so a value a handler or behavior writes into <c>Items</c> earlier in
/// the dispatch is still there for one read later in the same request. <see cref="LazyInitializer"/> guards
/// the one-time build against concurrent first access, for example from a pipeline behavior that fans work
/// out in parallel.
/// </para>
/// </remarks>
public sealed class AspNetCoreRequestContextAccessor(IHttpContextAccessor httpContextAccessor) : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private RequestContext? cached;

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

            return LazyInitializer.EnsureInitialized(ref this.cached, () => BuildContext(httpContext));
        }
        set => throw new NotSupportedException("Cannot set request context on web-backed accessor.");
    }

    private static RequestContext BuildContext(HttpContext httpContext)
    {
        // StringValues.ToString() returns "" for a missing (or explicitly empty) header, never null, so a
        // plain `?? fallback` can never trigger - IsNullOrEmpty is the correct absence check here.
        var correlationIdHeader = httpContext.Request.Headers["X-Correlation-ID"];
        var correlationId = StringValues.IsNullOrEmpty(correlationIdHeader)
            ? httpContext.TraceIdentifier
            : correlationIdHeader.ToString();

        var userId = httpContext.User?.Identity?.Name;

        var tenantIdHeader = httpContext.Request.Headers["X-Tenant-ID"];
        var tenantId = StringValues.IsNullOrEmpty(tenantIdHeader)
            ? httpContext.User?.FindFirst("tenant_id")?.Value
            : tenantIdHeader.ToString();

        return new RequestContext
        {
            CorrelationId = correlationId,
            UserId = userId,
            TenantId = tenantId,
        };
    }
}
