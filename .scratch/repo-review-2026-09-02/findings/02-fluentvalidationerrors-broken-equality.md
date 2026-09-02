Status: needs-triage
Severity: CRITICAL
Category: test-coverage / bug
Source: tests-conventions.md lane, confirmed empirically by antagonist lane (independent throwaway test + `git show` of the commit itself)

# `FluentValidationErrors` never got the order-independent equality it's claimed to have

## Summary

`FluentValidationErrors` relies on compiler-synthesized record-struct equality over a plain `IReadOnlyList<ValidationFailure>?` field — reference equality for the backing list, not content equality — despite `.scratch/outcome-value-equality/issues/01-order-independent-aggregate-equality.md` and commit `d5c9a80`'s own message explicitly claiming it (along with five other aggregate types) got content-based, order-independent equality "with no changes of their own."

## Evidence

`src/Interlocutor.FluentValidation/FluentValidationErrors.cs:30-116` is a `readonly record struct` over a bare `_failures` field. Its own code comment says outright it is "Not `DistinctItemSet<TItem>`-typed like the other five aggregate types' backing fields... this type preserves every failure verbatim rather than deduplicating." It has no `Equals`/`GetHashCode` override of its own.

Verified empirically (temporary test, reverted): `new FluentValidationErrors([failure])` compared against `new FluentValidationErrors([failure])` — same `ValidationFailure` instance, two different backing arrays — gives `equal=False; hashMatch=False`, even though both sides `ToString()` identically.

`git show --stat d5c9a80`'s commit message explicitly claims: "Failures, Failures\<TErrorCode\>, Conflicts, ValidationErrors, ValidationErrors\<TErrorCode\>, and FluentValidationErrors all compose this type as their only field, so they inherit correct order-independent equality automatically with no changes of their own" — a direct, verifiable factual error about `FluentValidationErrors` specifically. `ValidationErrors.cs`, `Failures.cs`, and `Conflicts.cs` do genuinely compose `DistinctItemSet<T>`; `FluentValidationErrors.cs` is the sole listed type that doesn't.

## Consequence

A handler that builds this failure case twice from equivalent validator output (a retried validation pass, or two requests failing the same way) gets a case value that silently fails `==`/`Assert.Equal` against an otherwise-identical instance. No test in `tests/Interlocutor.FluentValidation.Tests/FluentValidationErrorsTests.cs` exercises `Equals`/`GetHashCode`/`==` at all, so this shipped unnoticed.

## Suggested action

Either give `FluentValidationErrors` its own content-based `Equals`/`GetHashCode` (order-independent, consistent with the other five types), or compose `DistinctItemSet<ValidationFailure>` the way the others do if verbatim (non-deduplicated) ordering can be preserved through it. Then add the same order-independent-equality + hash-code-agreement + inverse (differing items unequal) + default-vs-explicit tests that ticket 01 required for the other five types — see [05-missing-direct-equality-tests-aggregate-types.md](05-missing-direct-equality-tests-aggregate-types.md), which covers the same gap for the other four aggregate types.
