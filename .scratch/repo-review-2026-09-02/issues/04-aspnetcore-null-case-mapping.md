# 04 - Fix/verify AspNetCore's outcome-case mapping for null payloads

**What to build:** `ToActionResult()` (and the underlying `IOutcomeStatusMapper` catch-all path) maps a null-payload success outcome to the correct HTTP response instead of reporting a bogus case type and either throwing an unhelpful reflection error or mapping it to a failure status.

Full technical detail: [findings/07-aspnetcore-null-case-mismapping.md](../findings/07-aspnetcore-null-case-mismapping.md).

**Blocked by:** 01 - Fix the `Outcome` null-payload/no-discriminator bug at the root.

**Status:** ready-for-agent

- [ ] `IOutcomeCaseRegistry.TryGetActiveCase` reports the real case type for a null-payload success (not the outcome's own container type)
- [ ] `ResultExtensions.ExtractCase`/`ToActionResult()` maps a null-payload success outcome to the correct HTTP response
- [ ] A regression test in `Interlocutor.AspNetCore`'s test project exercises this path specifically
