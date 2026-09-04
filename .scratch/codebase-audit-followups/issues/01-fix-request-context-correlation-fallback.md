# 01 - Fix correlation-ID/tenant-ID fallback in AspNetCoreRequestContextAccessor

**What to build:** `AspNetCoreRequestContextAccessor` should actually fall back to `HttpContext.TraceIdentifier` (for correlation ID) and to the appropriate claim (for tenant ID) when the corresponding request header is absent. Today `StringValues.ToString()` never returns `null` (it returns `""` when the header is missing), so the existing `?? fallback` never fires and callers silently get an empty string instead of the documented fallback value.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `CorrelationId` falls back to `HttpContext.TraceIdentifier` when the `X-Correlation-ID` header is absent or empty
- [x] The tenant ID accessor falls back to its documented claim-based value when the `X-Tenant-ID` header is absent or empty
- [x] A regression test exercises the "header absent" case for both properties and asserts the fallback value is used, not `""`
- [x] Existing `Interlocutor.AspNetCore.Tests` pass

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `AspNetCoreRequestContextAccessor` now checks `StringValues.IsNullOrEmpty` before falling back to `HttpContext.TraceIdentifier`, not a no-op `?? fallback` over a non-null empty string. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
