> **Status: done**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/trim-and-aot-support.md` and `documentation/testing.md` following the standing decisions in `../PRD.md`. `trim-and-aot-support.md` should state current trim-compatible-not-AOT-compatible status as fact without citing `maintainer_docs/adr/0016-...` (house style forbids internal-record citations from `documentation/`) - link sideways to `handler-selection-and-dispatch.md` or restate the reasoning directly instead. Verify testing guidance against the actual test projects' patterns (xunit.v3, NSubstitute).

## Acceptance criteria

- [x] No stale language remains
- [x] No citation to `maintainer_docs/` paths
- [x] Testing guidance matches actual test project conventions
- [x] Glossary-first-mention links added where appropriate

## Findings beyond the original scope

- `trim-and-aot-support.md` was verified thoroughly (trim-safety claims, `RequiresUnreferencedCode` annotations, `InterlocutorStartupValidation.ValidateHandlerRegistrations`) and found accurate as written - already correctly used `ToMinimalApiResult()`, ahead of the fix that landed in unit 05.
- `testing.md`'s first unit test read a union's `.Value` (typed `object`) directly as if it were the narrowed success type (`user.Name` on an `object` doesn't compile) - every other test in the file correctly narrows via `Assert.IsType<T>(result.Value)` first. Fixed the one outlier to match.
- `testing.md`'s "DI Startup Validation" test claimed `AddInterlocutor` + `BuildServiceProvider()` throws automatically for a missing handler with no opt-in call - directly contradicting the no-default-validation design confirmed repeatedly elsewhere (ADR-0009, `builder-and-registration.md`, unit 03's findings). Replaced it with two accurate tests: one exercising `ValidateOnBuild()` (throws `InterlocutorRegistrationValidationException`, not a bare `InvalidOperationException`), one exercising `ValidateHandlerRegistrations` after the container is built.
