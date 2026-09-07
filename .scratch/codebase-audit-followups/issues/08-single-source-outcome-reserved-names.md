# 08 - Single source of truth for the Outcome generator's reserved accessor names

> **Originally dropped - false premise (2026-09-04).** This ticket assumed `OutcomeAccessorCollisionAnalyzer` and the INTERLOC012 diagnostic already existed on the `2026-09-04-CodebaseAuditFollowUps` branch. Neither did - they existed only on the local `ultrareview` branch's unpushed commits (`cd21ada`, `4b63d4f`). No action was taken at the time; `ultrareview` was pushed to `origin/ultrareview` for safekeeping, independent of this ticket.

**What to build:** `OutcomeAccessorCollisionAnalyzer` maintains its own hand-typed `HashSet<string>` of reserved member names (`IsSuccess`, `ActiveCaseType`, `TryGetValue`, `Match`, `Switch`, `ToString`, `Equals`, `GetHashCode`) that must stay in sync with what `OutcomeMemberEmitter` actually emits, but nothing enforces that sync - a new fixed member added to the emitter can silently escape the INTERLOC012 collision diagnostic. Have `OutcomeMemberEmitter` expose its reserved-name set as one property (e.g. a static `ImmutableArray<string>`), and have the analyzer read that instead of re-declaring its own list.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `OutcomeMemberEmitter` exposes the complete set of names it always emits, computed in one place
- [x] `OutcomeAccessorCollisionAnalyzer` consumes that set instead of its own hand-typed `HashSet<string>`
- [x] A generator test asserts that every name the emitter can generate is covered by INTERLOC012's reserved set (so the two can no longer drift)
- [x] `Interlocutor.Generator.Tests` pass, including existing INTERLOC012 diagnostic tests

**2026-09-04** - Implemented: `OutcomeMemberEmitter` now exposes `public static ImmutableArray<string> ReservedMemberNames` (nine names: `Value`, `IsSuccess`, `ActiveCaseType`, `TryGetValue`, `Match`, `Switch`, `ToString`, `Equals`, `GetHashCode`), computed once with an XML doc explaining why each is reserved unconditionally. `OutcomeAccessorCollisionAnalyzer` dropped its own hand-typed `HashSet<string>` and reads that set instead. New `tests/Interlocutor.Generator.Tests/Emission/OutcomeMemberEmitterReservedNamesTests.cs` runs the generator, extracts every fixed name it actually emits from the generated syntax tree, and asserts each is covered by `ReservedMemberNames`. Full solution build and test (1190/1190) green.

## Comments

**2026-09-04** - Revived: `origin/ultrareview` has since been merged into `2026-09-04-ResearchTicketNormalization` (which supersedes `2026-09-04-CodebaseAuditFollowUps`), so `OutcomeAccessorCollisionAnalyzer` and INTERLOC012 now exist on this branch. Confirmed the underlying premise is true and the work is still undone: `OutcomeAccessorCollisionAnalyzer.ReservedMemberNames` (`src/Interlocutor.Generator/Analysis/OutcomeAccessorCollisionAnalyzer.cs`) is still a hand-typed `HashSet<string>`, and `OutcomeMemberEmitter` does not yet expose a shared reserved-name set. Status reset to `ready-for-agent`.
