# NSubstitute Quality Checklist

Design judgment best practices for NSubstitute. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] Every stubbed call that matters has a corresponding `Received` check — `Returns` without `Received` is dead configuration
- [ ] Specific matchers (`Arg.Is<T>(predicate)`) preferred over `Arg.Any<T>` when the expected argument value is knowable at test-write time
- [ ] `Received(1)` preferred over bare `Received()` for clarity that exactly one call is expected
- [ ] `.DidNotReceive()` used to assert absence — not just failing to call `Received` on a setup that isn't triggered
- [ ] `Substitute.ForPartsOf<T>` used sparingly — its presence suggests the design would benefit from extracting a dependency
- [ ] Callbacks (`.Returns(x => ...)`) used over `.When(...).Do(...)` for simple computed returns; reserve `.When().Do()` for side effects
- [ ] NSubstitute.Analyzers enabled — add `NSubstitute.Analyzers` NuGet to catch common mistakes at build time
