# 04 - Fix/verify AspNetCore's outcome-case mapping for null payloads

**What to build:** `ToActionResult()` (and the underlying `IOutcomeStatusMapper` catch-all path) maps a null-payload success outcome to the correct HTTP response instead of reporting a bogus case type and either throwing an unhelpful reflection error or mapping it to a failure status.

Full technical detail: [findings/07-aspnetcore-null-case-mismapping.md](../findings/07-aspnetcore-null-case-mismapping.md).

**Blocked by:** 01 - Fix the `Outcome` null-payload/no-discriminator bug at the root.

**Status:** done

- [x] `IOutcomeCaseRegistry.TryGetActiveCase` reports the real case type for a null-payload success (not the outcome's own container type)
- [x] `ResultExtensions.ExtractCase`/`ToActionResult()` maps a null-payload success outcome to the correct HTTP response
- [x] A regression test in `Interlocutor.AspNetCore`'s test project exercises this path specifically

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. DefaultOutcomeCaseRegistry.TryGetActiveCase reports the real case type for a null-payload success instead of the outcome's own container type. ResultExtensions' hand-written result.Value switch blocks were replaced with result.Match(...), sharing the fixed generated logic. tests/Interlocutor.AspNetCore.Tests/ResultExtensionsTests.cs has ToActionResult_WithNullPayloadSuccess_ReturnsOkObjectResultWithNullValue and a catch-all-path equivalent. No source changes were needed this session.
