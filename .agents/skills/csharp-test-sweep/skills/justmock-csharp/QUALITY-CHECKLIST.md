# JustMock Quality Checklist

Design judgment best practices for Telerik JustMock. Applied alongside the [General Quality Checklist](../../references/quality-checklist.md) and the [Framework Checklist](SKILL.md) (syntax correctness).

- [ ] Every arrangement that matters has a corresponding `Mock.Assert` with explicit `Occurs` — `Mock.Arrange` without assertion is dead configuration
- [ ] Specific matchers (`Arg.Matches<T>(p)`) preferred over `Arg.IsAny<T>` when the expected argument value is knowable at test-write time
- [ ] Elevated mode used only when necessary — Free mode (virtual-only) suffices for interface contracts and abstract dependencies
- [ ] `Mock.CreateLike` preferred over manual per-property `Mock.Arrange` when only default values are needed
- [ ] `Behavior.Strict` used sparingly — `Loose` + targeted `Mock.Assert` produces less brittle tests
- [ ] Callbacks (`DoInstead`) used for argument capture, not for assertion logic — assert captured values in the Assert section
