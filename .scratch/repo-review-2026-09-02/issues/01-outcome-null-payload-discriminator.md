# 01 - Fix the `Outcome` null-payload/no-discriminator bug at the root

**What to build:** A handler that legitimately succeeds (or fails) with a `null` case payload — a nullable reference type or a `Nullable<T>` with no value — is recognized correctly by every generated `Outcome` member. `IsSuccess` reports the correct case, `TryGetValue` succeeds for the right case, and `Match`/`Switch` dispatch to the right branch instead of throwing, for every `Outcome` arity (2 through 6).

Full technical detail and reproduction: [findings/01-outcome-null-payload-no-discriminator.md](../findings/01-outcome-null-payload-no-discriminator.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `Outcome`/`Outcome<...>` (every arity) can distinguish "case N set, value null" from "no case set" — a real discriminator/tag, or an equivalent sentinel-wrapping approach, not a bare type-pattern match against the boxed value
- [ ] `OutcomeMemberEmitter`'s generated `IsSuccess`, `TryGetValue`, `Match`, and `Switch` all consult the new discriminator correctly
- [ ] `Interlocutor.CommonKernel.Tests` covers a null-payload case for at least one arity, exercising `IsSuccess`, `TryGetValue`, `Match`, and `Switch`
- [ ] No existing `Outcome` behavior (non-null cases, equality, `ToString`) regresses
