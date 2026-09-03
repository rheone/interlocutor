# 02 - Fix `FluentValidationErrors` equality

**What to build:** Two `FluentValidationErrors` instances built from equal-content-but-different-instance failure lists compare equal via `==`/`Equals`/`Assert.Equal`, and produce matching hash codes — the same guarantee the other five aggregate result types (`Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`) already have.

Full technical detail and reproduction: [findings/02-fluentvalidationerrors-broken-equality.md](../findings/02-fluentvalidationerrors-broken-equality.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `FluentValidationErrors` compares equal (and hashes equal) for two instances holding the same failures regardless of which distinct collection instance backs them
- [ ] `FluentValidationErrors` compares unequal for instances holding different failures
- [ ] The verbatim, non-deduplicating semantics this type is documented to have (unlike the other five aggregate types) are preserved
- [ ] `tests/Interlocutor.FluentValidation.Tests/FluentValidationErrorsTests.cs` gets direct `Equals`/`GetHashCode`/`==` coverage
