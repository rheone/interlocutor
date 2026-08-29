Status: open

## Labels

ready-for-agent

## What to build

The guide (Step 5's note, and "What's next") deliberately assumes real authentication middleware already exists and populates `HttpContext.User` - it doesn't teach that part. This ticket is the small piece of non-Interlocutor plumbing this POC needs to actually be runnable and testable end to end: a minimal ASP.NET Core `AuthenticationHandler` that trusts an `X-User-Id` request header, but only once it's validated against a real `User` (ticket 03).

- `Infrastructure/DevHeaderAuthenticationHandler.cs` (or similar - pick a name that reads clearly as "not for production"): implements `AuthenticationHandler<AuthenticationSchemeOptions>`. Reads `X-User-Id` from the incoming request headers, parses it as the `User` entity's id, and looks it up via `IDomainRepository<User, int>`. No header, unparseable value, or no matching user → `AuthenticateResult.NoResult()`/`.Fail(...)` (whichever reads more clearly for an unauthenticated request) so the endpoint's own `[Authorize]`/`RequireAuthorization()` (or the guide's `contextAccessor.Context?.UserId ?? throw ...` pattern) does the right thing. A match → build a `ClaimsPrincipal` with the user's id as the `NameIdentifier` claim, exactly what `AspNetCoreRequestContextAccessor` (from `Interlocutor.AspNetCore`) already knows how to read - no custom `IRequestContextAccessor` needed, the guide's own accessor does the job once `HttpContext.User` is populated correctly.
- Wire it into `Program.cs`: `builder.Services.AddAuthentication(...).AddScheme<...>(...)`, `app.UseAuthentication()` before `app.UseAuthorization()` (add `UseAuthorization()` too if not already present from ticket 02), and `.RequireAuthorization()` on whichever endpoints need it (ticket 05 onward will apply this to the `TodoTask` write endpoints that need a caller identity).
- A short doc comment on the handler itself should say plainly that this is a development/testing stand-in for real authentication (JWT bearer, cookies, etc.) - the guide's own "Before you start" section already states the same rule for the wider library; this comment should say it for this specific class.

This ticket does not add any endpoints of its own - it's infrastructure that ticket 05 (and every authenticated `TodoTask` endpoint after it) depends on.

## Acceptance criteria

- [ ] `DevHeaderAuthenticationHandler` (or equivalently named type) exists, XML-documented, explicitly flagged as dev/test-only in its own doc comment
- [ ] A request with no `X-User-Id` header, or one that doesn't match an existing `User`, is unauthenticated
- [ ] A request with an `X-User-Id` matching a real `User.Id` is authenticated, and `IRequestContextAccessor.Context.UserId` (via the existing `AspNetCoreRequestContextAccessor`) reflects that user's id with no bespoke plumbing on the Interlocutor side
- [ ] Unit test(s) for the handler's three outcomes (missing header, unknown user, known user) using a fake/mocked `HttpContext` and repository
- [ ] HTTP integration test: a request against a placeholder `[Authorize]`-protected endpoint (or the first real one, if ticket 05 has already landed by the time this is verified) with a valid `X-User-Id` succeeds, and without one returns 401
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

03
