# 02 - Fix `FluentValidationErrors` equality

**What to build:** Two `FluentValidationErrors` instances built from equal-content-but-different-instance failure lists compare equal via `==`/`Equals`/`Assert.Equal`, and produce matching hash codes — the same guarantee the other five aggregate result types (`Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`) already have.

Full technical detail and reproduction: [findings/02-fluentvalidationerrors-broken-equality.md](../findings/02-fluentvalidationerrors-broken-equality.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `FluentValidationErrors` compares equal (and hashes equal) for two instances holding the same failures regardless of which distinct collection instance backs them
- [x] `FluentValidationErrors` compares unequal for instances holding different failures
- [x] The verbatim, non-deduplicating semantics this type is documented to have (unlike the other five aggregate types) are preserved
- [x] `tests/Interlocutor.FluentValidation.Tests/FluentValidationErrorsTests.cs` gets direct `Equals`/`GetHashCode`/`==` coverage

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. FluentValidationErrors.Equals/GetHashCode do a content-based, order-independent, duplicate-count-preserving multiset match (ValidationFailure has no equality of its own, so this can't reuse DistinctItemSet directly) - verbatim non-deduplicating semantics preserved. tests/Interlocutor.FluentValidation.Tests/FluentValidationErrorsTests.cs has direct Equals/GetHashCode/== coverage. No source changes were needed this session.
