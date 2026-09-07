# 02 - Source-generate `ResultExtensions`'s six per-arity overloads

**What to build:** `src/Interlocutor.AspNetCore/ResultExtensions.cs` hand-writes `ToActionResult()`/`ToMinimalApiResult()` once per `Outcome` arity (2 through 6) plus the non-generic `Outcome`, each block differing only in how many `caseN => (mapper ?? DefaultMapper).Map(caseN)...` arms it has. Move that per-arity dispatch to source generation, using `Interlocutor.Generator` as a `PrivateAssets="all"` analyzer reference from `Interlocutor.AspNetCore` itself - the same self-generation pattern `Interlocutor.CommonKernel` already uses for its own `Outcome` member generation, not the consumer-opt-in handler-discovery generator. The reflective `MapMethodCache`/`MakeGenericMethod` core, the null-payload disambiguation via `AsOutcomeCaseIsSuccess()`, and the IL2060/trimming documentation stay exactly as they are - only the six-arity boilerplate moves.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A generator emitter produces the arity 2-6 (plus non-generic) `ToActionResult()`/`ToMinimalApiResult()` overloads from one template
- [x] `Interlocutor.AspNetCore` references `Interlocutor.Generator` as a `PrivateAssets="all"` analyzer only - no new runtime dependency, nothing new ships in a consumer's build output
- [x] Generated output is behaviorally identical to the current hand-written overloads, including trimming/AOT annotations
- [x] Every existing test in `tests/Interlocutor.AspNetCore.Tests` (including `ResultExtensionsTests`) passes with no modification
- [x] Adding a 7th arity in the future requires touching only the template, not six hand-written blocks
- [x] No public API changes - `PublicAPI.Unshipped.txt` for `Interlocutor.AspNetCore` is untouched

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `ResultExtensionsGenerator`/`ResultExtensionsEmitter` now emit `ResultExtensions`'s per-arity overloads; `ResultExtensions.cs` itself no longer hand-writes any `ToActionResult`/`ToMinimalApiResult` blocks. Landed via `origin/ultrareview` commit `b2eb20b`, merged into this branch. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
