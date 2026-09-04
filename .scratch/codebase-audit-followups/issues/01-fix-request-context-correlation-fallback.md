# 01 - Fix correlation-ID/tenant-ID fallback in AspNetCoreRequestContextAccessor

**What to build:** `AspNetCoreRequestContextAccessor` should actually fall back to `HttpContext.TraceIdentifier` (for correlation ID) and to the appropriate claim (for tenant ID) when the corresponding request header is absent. Today `StringValues.ToString()` never returns `null` (it returns `""` when the header is missing), so the existing `?? fallback` never fires and callers silently get an empty string instead of the documented fallback value.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `CorrelationId` falls back to `HttpContext.TraceIdentifier` when the `X-Correlation-ID` header is absent or empty
- [ ] The tenant ID accessor falls back to its documented claim-based value when the `X-Tenant-ID` header is absent or empty
- [ ] A regression test exercises the "header absent" case for both properties and asserts the fallback value is used, not `""`
- [ ] Existing `Interlocutor.AspNetCore.Tests` pass
