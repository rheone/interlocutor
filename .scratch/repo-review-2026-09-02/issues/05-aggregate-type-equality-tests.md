# 05 - Add direct equality tests for the other 4 aggregate types

**What to build:** `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, and `Failures<TErrorCode>` each get the same direct equality-test coverage `OutcomeEqualityTests` already gives `Failures` — order-independent equality, hash-code agreement, inequality on differing items, and default-vs-explicit-instance behavior — proven against the types themselves, not only indirectly through a wrapping `Outcome`.

Full technical detail: [findings/05-missing-direct-equality-tests-aggregate-types.md](../findings/05-missing-direct-equality-tests-aggregate-types.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, `Failures<TErrorCode>` each have a direct order-independent-equality test with agreeing hash codes
- [x] Each has an inverse test proving extra/missing/differing items make instances unequal
- [x] Each has a default-vs-default (equal) and default-vs-explicit (unequal) test

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. tests/Interlocutor.CommonKernel.Tests/AggregateResultTypesEqualityTests.cs gives Conflicts, ValidationErrors, ValidationErrors<TErrorCode>, and Failures<TErrorCode> the same order-independent-equality/hash-code-agreement/inequality/default-vs-explicit coverage OutcomeEqualityTests already gave Failures. No source changes were needed this session.
