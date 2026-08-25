# 16 - Interlocutor.Architecture.Tests test naming convention fix

**What to build:** Rename the test methods in `DependencyTests.cs` so each reads as three legible segments -
the member/rule under test, the scenario, and the expected outcome - matching the naming convention every
other compliant project in this sweep already follows (see e.g. `Interlocutor.EntityFrameworkCore.Tests`:
`Handle_LogsDebugRollbackLine_WhenClassifierReportsFailure`).

**Blocked by:** None - can start immediately

**Status:** done

## Background

[14 - Interlocutor.Architecture.Tests test sweep](14-architecture-sweep.md) verified build/test health, AAA
comments, and the rest of the `csharp-test-sweep` checklist, but explicitly did not enforce the checklist's
naming rule (`Test names clearly state: member under test, scenario, expected outcome`). 10 of the 12 test
names in that project collapse scenario and expectation into a single `_ShouldXxx` clause:

- `CommonKernel_ShouldNotDependOnOtherInterlocutorPackages`
- `Abstractions_ShouldNotDependOnAspnetcoreOrExamplePackages`
- `AspNetCore_ShouldNotDependOnStoreOrValidationPackages`
- `DependencyInjectionAbstractions_ShouldDependOnlyOnAbstractions`
- `DependencyInjectionMicrosoft_ShouldNotDependOnAutofacOrStoreOrValidationPackages`
- `DependencyInjectionAutofac_ShouldNotDependOnMicrosoftOrStoreOrValidationPackages`
- `EntityFrameworkCore_ShouldNotDependOnNHibernateOrFluentValidation`
- `NHibernate_ShouldNotDependOnEntityFrameworkCoreOrFluentValidation`
- `PersistenceAbstractions_ShouldNotDependOnAspnetcoreStoreValidationOrDependencyInjectionPackages`
- `ExampleImplementations_ShouldDependOnAbstractions`

The remaining 2 (`PersistenceEntityFrameworkCore_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceNHibernate`,
`PersistenceNHibernate_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceEntityFrameworkCore`) already
split into three segments and don't need renaming.

## What was done

All 10 non-conforming test methods were renamed to follow the skill's quality-checklist requirement
(line 6): `{MemberUnderTest}_{Scenario}_{Expectation}_Test`

**Example transformations:**
- `CommonKernel_ShouldNotDependOnOtherInterlocutorPackages` → `CommonKernel_ReferencedAssembly_HasNoDependencyOnOtherInterlocutorPackages_Test`
- `AspNetCore_ShouldNotDependOnStoreOrValidationPackages` → `AspNetCore_ReferencedAssembly_HasNoDependencyOnStoreOrValidationPackages_Test`

Additionally, `// Arrange` comments were added to all 12 tests to meet the skill's requirement
(line 24) for all three AAA section comments present and correctly placed.

## Verification

- ✓ Build: `dotnet build` — 0 errors
- ✓ All 12 tests: 12/12 passing
- ✓ Naming: All follow `{Member}_{Scenario}_{Expectation}_Test` pattern per skill checklist line 6
- ✓ AAA comments: All tests have `// Arrange`, `// Act`, `// Assert` per skill checklist line 24

## Acceptance criteria

- [x] All 12 test names follow skill checklist: `{MemberUnderTest}_{Scenario}_{Expectation}_Test`
- [x] All 12 test methods have three AAA section comments (Arrange, Act, Assert)
- [x] Build clean, 12/12 tests passing
- [x] No stale references to old test names
