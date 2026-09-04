namespace TodoTasks.Api.Infrastructure;

using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Interlocutor.Persistence.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TodoTasks.Api.Domain;

/// <summary>
/// A development/testing stand-in for real authentication (JWT bearer, cookies, an identity
/// provider, ...) - never use anything shaped like this in a real application. Trusts an
/// <c>X-User-Id</c> request header as the caller's claimed identity, but only authenticates it once
/// it matches a real <see cref="User"/>, so this project's endpoints have a genuine authenticated
/// caller to work with without building actual authentication infrastructure - exactly the piece
/// <c>documentation/beginners-guide-todo-api.md</c>'s "Before you start" section says the guide
/// itself assumes already exists and deliberately doesn't teach.
/// </summary>
/// <remarks>
/// Populates <see cref="ClaimsIdentity"/> with the matched user's id as the <see cref="ClaimTypes.Name"/>
/// claim specifically - not <see cref="ClaimTypes.NameIdentifier"/> - because
/// <c>Interlocutor.AspNetCore.AspNetCoreRequestContextAccessor</c> (verified against its shipped
/// source, not assumed) reads <c>RequestContext.UserId</c> from <c>HttpContext.User.Identity.Name</c>,
/// which <see cref="ClaimsIdentity"/> resolves from the <see cref="ClaimTypes.Name"/> claim by
/// default. Get this claim type wrong and every endpoint's caller-identity lookup silently reads
/// <see langword="null"/> instead of throwing - the same "no authenticated caller" shape a genuinely
/// anonymous request produces, so the bug hides behind seemingly-correct 401s instead of failing loud.
/// </remarks>
public sealed class DevHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDomainRepository<User, int> users
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The authentication scheme name this handler registers under.</summary>
    public const string SchemeName = "DevHeader";

    /// <summary>The request header this handler reads the caller's claimed user id from.</summary>
    public const string HeaderName = "X-User-Id";

    /// <inheritdoc/>
    /// <remarks>
    /// Three outcomes, all via <see cref="AuthenticateResult.NoResult"/> for "not authenticated" (not
    /// <see cref="AuthenticateResult.Fail(string)"/>, which is for a scheme-level error, not simply "this
    /// caller didn't present valid credentials"): no header, a header that doesn't parse as an
    /// <see cref="int"/>, or a well-formed id that doesn't match any <see cref="User"/> all leave the
    /// request unauthenticated - letting <c>RequireAuthorization()</c> reject it with a 401 the normal
    /// way, rather than this handler special-casing "invalid" differently from "absent."
    /// </remarks>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (
            !this.Request.Headers.TryGetValue(HeaderName, out var headerValues)
            || !int.TryParse(headerValues.ToString(), out var userId)
        )
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (users.GetById(userId) is not { } user)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, user.Id.ToString(CultureInfo.InvariantCulture))],
            this.Scheme.Name
        );
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
