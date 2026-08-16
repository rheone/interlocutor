# NUnit Quality Checklist

Design judgment best practices for NUnit v5. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] Constraint composition (`&` / `|`) preferred over nested `Assert.That` calls for multi-condition checks — e.g. `Assert.That(result, Is.GreaterThan(0) & Is.LessThan(100))`
- [ ] `Does.Contain` preferred over `StringAssert.Contains` for consistency within the constraint model
- [ ] `TestCaseData.SetName()` used to give meaningful display names to parameterized test cases
- [ ] Classic `Assert.AreEqual` avoided in new code — use `Assert.That(actual, Is.EqualTo(expected))` instead
- [ ] `[Retry]` used sparingly and only for flaky infrastructure interaction — never to mask non-deterministic test logic
- [ ] `[Parallelizable(ParallelScope.Children)]` used over individual `[NonParallelizable]` to express the default intent
- [ ] NUnit Analyzers enabled — add `NUnit.Analyzers` NuGet to catch classic-to-constraint migration gaps at build time
