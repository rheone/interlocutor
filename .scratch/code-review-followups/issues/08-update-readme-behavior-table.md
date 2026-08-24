# 08 - Update README.md's package table for the new pipeline behaviors

**What to build:** README.md's package table (documenting `Interlocutor.Abstractions`) still lists only `LoggingBehavior`, `AuthorizationBehavior`, `AuditingBehavior` as its pipeline behaviors, even though `CancellationBehavior`, `TimeoutBehavior`, and `RateLimitedBehavior` were added as freestanding, opt-in behaviors and are already fully covered in `documentation/pipeline-and-behaviors.md` and `documentation/glossary.md`. The sibling docs are inconsistent with README.md.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] README.md's package table for `Interlocutor.Abstractions` mentions `CancellationBehavior`, `TimeoutBehavior`, and `RateLimitedBehavior` alongside the existing three, noting they're opt-in with no prescribed position (consistent with `UseRecommendedChain()`'s unchanged five-behavior order)
