# Moq Quality Checklist

Design judgment best practices for Moq. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] Every setup that matters has a corresponding `Verify` — setup without verification is dead configuration
- [ ] Specific matchers (`It.Is<T>(predicate)`) preferred over `It.IsAny<T>` when the expected argument value is knowable at test-write time
- [ ] `Times.Once()` preferred over `Times.AtLeastOnce()` unless the call count is genuinely variable
- [ ] Unnecessary `.Setup` removed — if a call is never verified, don't arrange it
- [ ] `mock.Verify` used over state-based assertions when testing interaction contracts at dependency boundaries
- [ ] `MockBehavior.Strict` used sparingly — `Loose` + targeted `Verify` produces less brittle tests
- [ ] `Callback` used for argument capture, not for assertion logic — assert captured values separately in the Assert section
- [ ] Moq.Analyzers enabled — add `Moq.Analyzers` NuGet to catch setup/verification signature mismatches at build time
