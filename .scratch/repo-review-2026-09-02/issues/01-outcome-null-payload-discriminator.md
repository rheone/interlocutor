# 01 - Fix the `Outcome` null-payload/no-discriminator bug at the root

**What to build:** A handler that legitimately succeeds (or fails) with a `null` case payload — a nullable reference type or a `Nullable<T>` with no value — is recognized correctly by every generated `Outcome` member. `IsSuccess` reports the correct case, `TryGetValue` succeeds for the right case, and `Match`/`Switch` dispatch to the right branch instead of throwing, for every `Outcome` arity (2 through 6).

Full technical detail and reproduction: [findings/01-outcome-null-payload-no-discriminator.md](../findings/01-outcome-null-payload-no-discriminator.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `Outcome`/`Outcome<...>` (every arity) can distinguish "case N set, value null" from "no case set" — a real discriminator/tag, or an equivalent sentinel-wrapping approach, not a bare type-pattern match against the boxed value
- [x] `OutcomeMemberEmitter`'s generated `IsSuccess`, `TryGetValue`, `Match`, and `Switch` all consult the new discriminator correctly
- [x] `Interlocutor.CommonKernel.Tests` covers a null-payload case for at least one arity, exercising `IsSuccess`, `TryGetValue`, `Match`, and `Switch`
- [x] No existing `Outcome` behavior (non-null cases, equality, `ToString`) regresses

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce` ("fix: resolve Outcome null-payload discriminator and 16 follow-on tickets"), an ancestor of current HEAD. OutcomeMemberEmitter resolves the active case via a new GetActiveCaseIndex() helper: a non-null Value still disambiguates by type-pattern match; a null Value is attributed to the first-declared case type whose own default is null, only falling back to "unset" when no case type accepts null. IsSuccess/TryGetValue/Match/Switch and a new IOutcomeCase interface all route through it. tests/Interlocutor.CommonKernel.Tests/OutcomeNullPayloadTests.cs covers the null-payload case across all four members plus equality/ToString regression. 1183/1183 tests pass. No source changes were needed this session.
