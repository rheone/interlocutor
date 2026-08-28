> **Status: done** - merged in `0d8893d`.

# 05 - New case types: Cancelled, Timeout, RateLimited

## Labels

ready-for-agent

**What to build:** Add three new singular-only case types to `Interlocutor.CommonKernel.ResultTypes`, filling gaps the existing six cases (`Success`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`) can't express - none of these are meant to parallel HTTP status codes on purpose:

- `Cancelled(string? Reason = null)` - cooperative cancellation mid-flight. Reason-only (no richer payload - a `CancellationToken` carries no queryable reason in .NET), matching `NotFound`/`Forbidden`'s shape.
- `Timeout(TimeSpan Budget, TimeSpan Elapsed, string? Message = null)` - operation exceeded its time budget; carries both the declared budget and actual elapsed time.
- `RateLimited(string LimitKey, TimeSpan? RetryAfter = null, string? Message = null)` - caller was throttled; `LimitKey` identifies which limiter/bucket was hit, `RetryAfter` is generic retry guidance (no HTTP-specific naming - `Retry-After` header framing belongs only in `aspnetcore-integration.md`, in a later ticket).

Each gets a `ToString()` override per the shared convention (`"TypeName: detail"` / bare `"TypeName"`), and all three are seeded into `OutcomeClassifierBuilder` as failure types.

**Blocked by:** 02 (rename AspNetCore test fixture, so `RateLimited` doesn't collide when it lands)

- [x] `Cancelled`, `Timeout`, `RateLimited` exist in `src/Interlocutor.CommonKernel/ResultTypes/` with the shapes above
- [x] Each has XML doc comments matching the existing case types' style, including a remark on `Cancelled` explaining why it's reason-only (not an oversight relative to `Timeout`/`RateLimited`'s richer shape)
- [x] Each has a `ToString()` override: `Cancelled` → `"Cancelled"` / `"Cancelled: {Reason}"`; `Timeout` → includes `Budget` and `Elapsed`; `RateLimited` → includes `LimitKey`
- [x] `OutcomeClassifierBuilder`'s constructor registers all three as failure types (`RegisterFailureType<Cancelled>("Cancelled")`, etc.)
- [x] Unit tests cover construction, defaults, and `ToString()` for all three
- [x] `dotnet build` (whole solution, confirming the AspNetCore test fixture rename in ticket 02 already prevents any collision) and `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` pass
