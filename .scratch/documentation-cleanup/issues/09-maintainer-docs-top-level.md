> **Status: done**

## Labels

ready-for-agent

## What to build

Post-rename accuracy pass over `maintainer_docs/README.md`, `maintainer_docs/PRD.md`, `maintainer_docs/TODO.md`. `PRD.md` claims to be "the current specification" - verify its package structure, type catalog, and behavior descriptions still match `src/` exactly (a spec that's drifted from what shipped is worse than no spec). `TODO.md` is intentionally near-empty ("all promoted to tickets") - leave that framing, just confirm it's still true. This is maintainer-facing content, so forward-looking notes stay; only fix what's actually wrong or confusing.

## Acceptance criteria

- [x] `maintainer_docs/PRD.md` matches current `src/` exactly wherever it makes a concrete claim
- [x] `maintainer_docs/README.md`'s links and file list are accurate post-rename
- [x] `maintainer_docs/TODO.md`'s "nothing outstanding" framing is confirmed still true or updated

## Findings beyond the original scope

`maintainer_docs/PRD.md` was the most drifted file in the entire sweep - it described a bootstrap-era version of the library, not the shipped one:

- Type catalog listed `ValidationFailed`/`RuleViolation` types that don't exist (real: `ValidationError`/`ValidationErrors`, `Conflict`), and described `Failure` as a *union* of NotFound/Forbidden/etc. sub-cases - the real `Failure` is an independent reason-only record struct, one case type among several usable directly in an `OperationOutcome<...>` arity.
- Package structure still listed the retired `Interlocutor.EntityFrameworkCore`/`.NHibernate` packages and omitted `Interlocutor.Persistence.*`, `Interlocutor.DependencyInjection.Abstractions`, and `Interlocutor.Generator` entirely.
- Pipeline behavior order was wrong (Validation and Authorization swapped relative to the actual documented recommended chain), and described dispatch via `Activator.CreateInstance`-based `HandlerWrapper`/`VoidHandlerWrapper` - the shipped mechanism is compile-time source-generated dispatch (ADR-0008), the opposite design.
- Startup validation section claimed handler registration is checked automatically "at startup" - it's opt-in via three independent mechanisms, none automatic (confirmed repeatedly across units 03 and 06).
- HTTP mapping table was entirely superseded by unit 05's findings (wrong statuses, wrong/missing cases).

Rewrote the file end to end against everything verified in units 01-08, replacing the file-local "Comparison with bootstrap PRD" section (itself a staleness risk, duplicating what the ADR index already tracks) with a pointer to `README.md`'s ADR index. Also fixed one summary line in `maintainer_docs/README.md`'s ADR-0001 entry citing `Result<T>`, the pre-rename type name the (immutable) ADR itself still uses - added a parenthetical noting the shipped rename to `OperationOutcome<...>` without touching the ADR.

`maintainer_docs/TODO.md` confirmed still accurate as a near-empty placeholder - no change needed.
