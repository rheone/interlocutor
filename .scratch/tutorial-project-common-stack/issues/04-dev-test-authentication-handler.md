# 04 - Dev/test authentication handler

**What to build:** A minimal dev/test authentication handler that reads `X-User-Id` and validates it against the Users store - no matching user, no auth. Wired through `AspNetCoreRequestContextAccessor` (from `Interlocutor.AspNetCore`) exactly as the guide describes; nothing custom on the Interlocutor side.

**Blocked by:** 03

**Status:** done

- [x] A request with a valid `X-User-Id` for an existing user authenticates successfully
- [x] A request with a missing or unknown `X-User-Id` is rejected
- [x] HTTP integration tests exercise both paths via `WebApplicationFactory`

## Comments

**2026-09-04** - Implemented: `DevHeaderAuthenticationHandler` reads `X-User-Id`, looks it up via `IDomainRepository<User, int>`, and on a match builds a `ClaimsPrincipal` with a `ClaimTypes.Name` claim (the claim `AspNetCoreRequestContextAccessor.BuildContext` reads off `HttpContext.User.Identity.Name`) - missing/non-integer/unknown-user all return `AuthenticateResult.NoResult()`. Wired into `Program.cs` via `AddAuthentication`/`AddScheme` + `AddAuthorization`/`UseAuthentication`/`UseAuthorization`. Matching the old project's precedent, `/users` and `/health` stay anonymous; a new `/health/whoami` diagnostic endpoint requires authorization as the seam these tests exercise (no TodoTasks endpoints exist yet in this rebuild to enforce auth against for real - that lands with the TodoTasks feature tickets). Unit tests (mocked repository) and HTTP integration tests (via `TodoTasksApiFactory`) cover both the success and rejection paths. Full solution build (0 errors) and test (1083/1083) green.
