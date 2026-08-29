Status: done

## Labels

ready-for-agent

## What to build

The guide (Step 5's note, and "What's next") deliberately assumes real authentication middleware already exists and populates `HttpContext.User` - it doesn't teach that part. This ticket is the small piece of non-Interlocutor plumbing this POC needs to actually be runnable and testable end to end: a minimal ASP.NET Core `AuthenticationHandler` that trusts an `X-User-Id` request header, but only once it's validated against a real `User` (ticket 03).

- `Infrastructure/DevHeaderAuthenticationHandler.cs` (or similar - pick a name that reads clearly as "not for production"): implements `AuthenticationHandler<AuthenticationSchemeOptions>`. Reads `X-User-Id` from the incoming request headers, parses it as the `User` entity's id, and looks it up via `IDomainRepository<User, int>`. No header, unparseable value, or no matching user → `AuthenticateResult.NoResult()`/`.Fail(...)` (whichever reads more clearly for an unauthenticated request) so the endpoint's own `[Authorize]`/`RequireAuthorization()` (or the guide's `contextAccessor.Context?.UserId ?? throw ...` pattern) does the right thing. A match → build a `ClaimsPrincipal` whose `Identity.Name` is the user's id - **not** a `NameIdentifier` claim (checked against the actual shipped source, not assumed): `Interlocutor.AspNetCore.AspNetCoreRequestContextAccessor` populates `RequestContext.UserId` from `httpContext.User?.Identity?.Name` specifically, i.e. the `ClaimTypes.Name` claim (`ClaimsIdentity`'s default name-claim type). No custom `IRequestContextAccessor` needed either way - the guide's own accessor does the job once `HttpContext.User` is populated correctly.
- Wire it into `Program.cs`: `builder.Services.AddAuthentication(...).AddScheme<...>(...)`, `app.UseAuthentication()` before `app.UseAuthorization()` (add `UseAuthorization()` too if not already present from ticket 02), and `.RequireAuthorization()` on whichever endpoints need it (ticket 05 onward will apply this to the `TodoTask` write endpoints that need a caller identity).
- A short doc comment on the handler itself should say plainly that this is a development/testing stand-in for real authentication (JWT bearer, cookies, etc.) - the guide's own "Before you start" section already states the same rule for the wider library; this comment should say it for this specific class.

This ticket does not add any endpoints of its own - it's infrastructure that ticket 05 (and every authenticated `TodoTask` endpoint after it) depends on.

## Acceptance criteria

- [x] `DevHeaderAuthenticationHandler` exists, XML-documented, explicitly flagged as dev/test-only in its own doc comment
- [x] A request with no `X-User-Id` header, or one that doesn't match an existing `User`, is unauthenticated
- [x] A request with an `X-User-Id` matching a real `User.Id` is authenticated, and `IRequestContextAccessor.Context.UserId` (via the existing `AspNetCoreRequestContextAccessor`) reflects that user's id with no bespoke plumbing on the Interlocutor side
- [x] Unit test(s) for the handler's three outcomes (missing header, unknown user, known user, plus a fourth for an unparsable header) using a fake `HttpContext` and mocked repository
- [x] HTTP integration test: `GET /health/whoami` (a new, permanently-useful diagnostic endpoint, not a throwaway - see `## Comments`) with a valid `X-User-Id` succeeds and echoes the caller's id; without one, or with one that matches no user, returns 401
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (845/845 solution-wide)

## Blocked by

03

## Comments

- Corrected one assumption in this ticket's own draft before implementing: `AspNetCoreRequestContextAccessor` reads `RequestContext.UserId` from `HttpContext.User.Identity.Name`, **not** a `NameIdentifier` claim (checked against its shipped source in `Interlocutor.AspNetCore`, not assumed) - so the handler populates a `ClaimTypes.Name` claim. Verified empirically via a unit test asserting `result.Principal.Identity.Name` directly, not just that authentication "succeeded."
- No placeholder throwaway endpoint - added `GET /health/whoami` as a permanent, small diagnostic twin to `/health` (echoes the authenticated caller's id, `RequireAuthorization()`-gated), since ticket 05 (the first real authenticated endpoint) hadn't landed yet at this point in the sequence and a real app commonly wants a "whoami" endpoint like this anyway.
