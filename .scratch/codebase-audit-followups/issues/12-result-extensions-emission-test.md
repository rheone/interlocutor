# 12 - Add a generator-level emission test for ResultExtensionsEmitter

> **Originally dropped - false premise (2026-09-04).** This ticket assumed `ResultExtensionsEmitter` already existed on the `2026-09-04-CodebaseAuditFollowUps` branch. It didn't - it existed only on the local `ultrareview` branch's unpushed commit `b2eb20b`. No action was taken at the time; `ultrareview` was pushed to `origin/ultrareview` for safekeeping, independent of this ticket.

**What to build:** `ResultExtensionsEmitter` collapsed six hand-written, near-identical per-arity `extension` blocks (`ToActionResult`/`ToMinimalApiResult`) into a generator, but the test suite covering that surface (`ResultExtensionsTests.cs`, `MinimalApiResultExtensionsTests.cs`, `GenericCatchAllResultExtensionsTests.cs` - ~1100 lines combined) still hand-duplicates coverage per arity, with no generator-level emission test analogous to `OutcomeMemberGeneratorTests.cs`. Add a table-driven emission test for `ResultExtensionsEmitter` (mirroring `OutcomeMemberGeneratorTests.cs`'s approach) covering arities 2 through 6 plus void, then shrink the three downstream `Interlocutor.AspNetCore.Tests` files to representative-arity + edge-case coverage (arity 2, arity 6, void, custom mapper, the reflective catch-all) rather than exhaustively re-testing all six arities by hand against the compiled assembly.

**Blocked by:** None - can start immediately

**Status:** done

- [x] A `ResultExtensionsEmitter` emission test exists, table-driven over arities 2-6 plus void, asserting on generated source shape/compiled behavior via the existing generator test harness
- [x] `ResultExtensionsTests.cs`, `MinimalApiResultExtensionsTests.cs`, and `GenericCatchAllResultExtensionsTests.cs` are reduced to representative-arity and edge-case tests, with no loss of the distinct behaviors they were each verifying
- [x] `Interlocutor.Generator.Tests` and `Interlocutor.AspNetCore.Tests` pass

**2026-09-04** - Implemented: new `tests/Interlocutor.Generator.Tests/Emission/ResultExtensionsEmitterTests.cs` (table-driven, arities 2-6 plus void) asserts both generated source shape and compiled/runtime dispatch via reflection + `GeneratorTestHarness`, mirroring `OutcomeMemberGeneratorTests.cs`'s pattern. The three downstream `Interlocutor.AspNetCore.Tests` files shrunk to representative-arity (2, 6) plus void/custom-mapper/catch-all coverage, dropping only the redundant arity-3/4/5 hand-duplication - no distinct behavior lost. Full solution build and test (1183/1183) green.

## Comments

**2026-09-04** - Revived: `origin/ultrareview` has since been merged into `2026-09-04-ResearchTicketNormalization` (which supersedes `2026-09-04-CodebaseAuditFollowUps`), so `ResultExtensionsEmitter` (`src/Interlocutor.Generator/Emission/ResultExtensionsEmitter.cs`) now exists and `ResultExtensions.cs` is fully generated. Confirmed no emission test exists yet under `tests/Interlocutor.Generator.Tests` for it. Status reset to `ready-for-agent`.
