Status: needs-triage
Severity: HIGH
Category: test-coverage
Source: tests-conventions.md lane, confirmed by antagonist lane

# No direct equality/hashcode tests on `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, `Failures<TErrorCode>`

## Summary

`.scratch/outcome-value-equality/issues/01-order-independent-aggregate-equality.md`'s acceptance criteria require order-independent-equality + hash-code-agreement coverage for at least `Failures` and one generic (`<TErrorCode>`) variant, coverage of the inverse (differing items are unequal), and default-vs-explicit-instance coverage. None of these are met for `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, or `Failures<TErrorCode>`.

## Evidence

`tests/Interlocutor.CommonKernel.Tests/AggregateResultTypesTests.cs` has zero `Assert.Equal`/`GetHashCode` calls on the aggregate types themselves — only on their `Items`/`ToString()` output. The sole order-independent-equality proof in the whole solution, `OutcomeEqualityTests.Outcome_PairedWithFailures_IsEqualRegardlessOfTheOrderItsItemsWereCollectedIn`, tests `Failures` only, and only indirectly through `Outcome<string, Failures>.Equals` delegating to `Value.Equals` — it never calls `Failures.Equals` directly, never covers a `<TErrorCode>` generic variant, never tests the "differing items unequal" inverse, and never tests default-vs-explicit-instance equality.

A regression that broke `Conflicts.Equals`, `ValidationErrors<TErrorCode>.GetHashCode`, or the "same default, different explicit" invariant on any of these four types would ship with the full test suite green — this is exactly the blind spot that let [02-fluentvalidationerrors-broken-equality.md](02-fluentvalidationerrors-broken-equality.md) ship unnoticed.

## Suggested action

Add direct `Equals`/`GetHashCode`/`==` tests to `AggregateResultTypesTests.cs` (or a new equality-focused file alongside it) for `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, and `Failures<TErrorCode>`, covering: order-independent equality with agreeing hash codes, inequality on extra/missing/differing items, and default-vs-default (equal) vs. default-vs-explicit (unequal) — the same shape of coverage `OutcomeEqualityTests` already gives the `Outcome` union.
