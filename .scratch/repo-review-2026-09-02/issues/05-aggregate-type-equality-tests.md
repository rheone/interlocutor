# 05 - Add direct equality tests for the other 4 aggregate types

**What to build:** `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, and `Failures<TErrorCode>` each get the same direct equality-test coverage `OutcomeEqualityTests` already gives `Failures` — order-independent equality, hash-code agreement, inequality on differing items, and default-vs-explicit-instance behavior — proven against the types themselves, not only indirectly through a wrapping `Outcome`.

Full technical detail: [findings/05-missing-direct-equality-tests-aggregate-types.md](../findings/05-missing-direct-equality-tests-aggregate-types.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, `Failures<TErrorCode>` each have a direct order-independent-equality test with agreeing hash codes
- [ ] Each has an inverse test proving extra/missing/differing items make instances unequal
- [ ] Each has a default-vs-default (equal) and default-vs-explicit (unequal) test
