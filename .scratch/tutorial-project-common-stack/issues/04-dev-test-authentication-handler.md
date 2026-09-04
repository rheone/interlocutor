# 04 - Dev/test authentication handler

**What to build:** A minimal dev/test authentication handler that reads `X-User-Id` and validates it against the Users store - no matching user, no auth. Wired through `AspNetCoreRequestContextAccessor` (from `Interlocutor.AspNetCore`) exactly as the guide describes; nothing custom on the Interlocutor side.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] A request with a valid `X-User-Id` for an existing user authenticates successfully
- [ ] A request with a missing or unknown `X-User-Id` is rejected
- [ ] HTTP integration tests exercise both paths via `WebApplicationFactory`
