# 08 - DROPPED: Single source of truth for the Outcome generator's reserved accessor names

> **Dropped - false premise.** This ticket assumed `OutcomeAccessorCollisionAnalyzer` and the INTERLOC012 diagnostic already exist on this branch. Investigation during implementation found neither exists anywhere on `main` or this branch - they exist only on the local `ultrareview` branch's unpushed-until-now commits (`cd21ada`, `4b63d4f`), which were never merged. The originating architecture-review candidate was based on inspecting that branch's content without correctly attributing it as not-yet-merged. No action taken; `ultrareview` has since been pushed to `origin/ultrareview` for safekeeping, independent of this ticket.


**What to build:** `OutcomeAccessorCollisionAnalyzer` maintains its own hand-typed `HashSet<string>` of reserved member names (`IsSuccess`, `ActiveCaseType`, `TryGetValue`, `Match`, `Switch`, `ToString`, `Equals`, `GetHashCode`) that must stay in sync with what `OutcomeMemberEmitter` actually emits, but nothing enforces that sync - a new fixed member added to the emitter can silently escape the INTERLOC012 collision diagnostic. Have `OutcomeMemberEmitter` expose its reserved-name set as one property (e.g. a static `ImmutableArray<string>`), and have the analyzer read that instead of re-declaring its own list.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `OutcomeMemberEmitter` exposes the complete set of names it always emits, computed in one place
- [ ] `OutcomeAccessorCollisionAnalyzer` consumes that set instead of its own hand-typed `HashSet<string>`
- [ ] A generator test asserts that every name the emitter can generate is covered by INTERLOC012's reserved set (so the two can no longer drift)
- [ ] `Interlocutor.Generator.Tests` pass, including existing INTERLOC012 diagnostic tests
