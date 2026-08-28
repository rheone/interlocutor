# 14 - Interlocutor.Architecture.Tests test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Discovery findings

- Test framework: xUnit v3 (`xunit.v3`), plus `NetArchTest.Rules` for the assertions themselves - no mocking library (`net11.0` only) - no .NET Framework runner question applied.
- 1 test file (`DependencyTests.cs`), 12 test methods, all in one class - this project has no production code of its own to map tests to; it references every `src/*` project and asserts the cross-package dependency rules documented in `CLAUDE.md` and enforced for CI, matching the description in `tests/Interlocutor.Architecture.Tests` (the project referenced by `DependencyTests.cs` itself, see the class's assembly fields).
- Duplicate test IDs: none (`--list-tests` output had zero duplicate display names).
- Skipped tests: none found (`[Fact(Skip` / `[Theory(Skip` grep empty).
- `async void` tests: none found - every test is synchronous `void` (NetArchTest's `GetResult()` is synchronous).
- Assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.): none found. Every test uses `Assert.True(result.IsSuccessful, ...)` with an explanatory message built from `FailingTypeNames` - this is not the flagged `Assert.True(x == y)` boolean-comparison anti-pattern; it's the correct idiom for `NetArchTest.Rules`, which exposes no dedicated assertion helper of its own.
- `TheoryData<T>` usage: none in this project (no `[Theory]` methods at all - every test is a `[Fact]`).
- `InternalsVisibleTo`: not needed - no `internal` production types are under test; this project asserts assembly-to-assembly dependency edges via reflection (`Types.InAssembly(...)`), not member access.
- `[Obsolete]` members: none in this package's source - the "test Obsolete members" config had nothing to act on.
- Project hygiene (`.csproj`): `IsPackable` not explicitly set (consistent with sibling test projects - SDK conventions handle it); package references (xunit.v3, xunit.runner.visualstudio, Microsoft.Testing.Platform, Microsoft.NET.Test.Sdk, coverlet.collector, NetArchTest.Rules) are current, no outdated xUnit v1/v2 or MSTest v1/v2 references.
- Duplicate-test-pattern candidates (literal-only-different `[Fact]`s): none found warranting `[Theory]` conversion - each test asserts a distinct pair (or small group) of package-dependency edges, not a parameterizable single assertion shape.
- Real infrastructure: none - all 12 tests operate purely on already-built assembly metadata via reflection (`typeof(...).Assembly`) and `NetArchTest.Rules`; no database/file/network dependency.

## What was done

Baseline (before any edits): `dotnet build` clean (0 errors; only pre-existing `src/` StyleCop warnings unrelated to this project), full test run 12/12 passing. No coverage gaps were found - every existing test method was already exercising real behavior (the cross-package dependency rules from `CLAUDE.md`), so no new `// Auto Generated, verify expected behavior:` tests were needed.

Retrofitted `// Act` / `// Assert` section comments onto all 12 test methods in `DependencyTests.cs`:

- `CommonKernel_ShouldNotDependOnOtherInterlocutorPackages`
- `Abstractions_ShouldNotDependOnAspnetcoreOrExamplePackages`
- `AspNetCore_ShouldNotDependOnStoreOrValidationPackages`
- `DependencyInjectionAbstractions_ShouldDependOnlyOnAbstractions`
- `ExampleImplementations_ShouldDependOnAbstractions`
- `DependencyInjectionMicrosoft_ShouldNotDependOnAutofacOrStoreOrValidationPackages`
- `DependencyInjectionAutofac_ShouldNotDependOnMicrosoftOrStoreOrValidationPackages`
- `EntityFrameworkCore_ShouldNotDependOnNHibernateOrFluentValidation`
- `NHibernate_ShouldNotDependOnEntityFrameworkCoreOrFluentValidation`
- `PersistenceAbstractions_ShouldNotDependOnAspnetcoreStoreValidationOrDependencyInjectionPackages`
- `PersistenceEntityFrameworkCore_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceNHibernate`
- `PersistenceNHibernate_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceEntityFrameworkCore`

No `// Arrange` comments were added - see Ambiguities below for why. Two tests (`DependencyInjectionAbstractions_ShouldDependOnlyOnAbstractions` and the two `Persistence*` reciprocal-check tests) run a second logically-distinct Act/Assert pair within the same method to check a related edge; those got a second labeled `// Act (second check)` / `// Assert (second check)` pair rather than being split into separate test methods, since splitting would have gone beyond the sweep's AAA-retrofit/gap-fill scope.

## Ambiguities / annotations left in place

None requiring `#warning` or `// SWEEP-AMBIGUITY:`. One deliberate deviation from the general checklist: no test in this class has an `// Arrange` section. All fixture data (`Assembly` references for every package under test) are static `readonly` fields at the class level, computed once for the whole class rather than per-method - there is no per-test setup step to label. Adding an empty `// Arrange` comment above the `// Act` line in every method would have been noise without content, so it was omitted rather than added as a no-op marker.

## Flagged but not fixed

None found warranting a flag - the class is small, single-purpose, and every test already asserted real, current architecture rules.

## Verification

- Baseline: `dotnet build tests/Interlocutor.Architecture.Tests/Interlocutor.Architecture.Tests.csproj` - 0 errors; full run via the built executable - **12/12 passed, 0 failed, 0 skipped** (823ms).
- Post-edit: rebuilt clean (0 errors), re-ran the built executable - **12/12 passed, 0 failed, 0 skipped**.
- Note on tooling: `dotnet test` in this sandbox fails with `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later` (same pre-existing environment/SDK-preview issue noted in ticket 06, unrelated to this sweep). Verification instead used `dotnet build` followed by running the built Microsoft.Testing.Platform executable directly: `./tests/Interlocutor.Architecture.Tests/bin/Debug/net11.0/Interlocutor.Architecture.Tests`.

- [x] Sweep applied to tests/Interlocutor.Architecture.Tests
- [x] Build passes
- [x] Full test run passes

## Follow-ups

None requiring human judgment. The `dotnet test` CLI tooling gap noted in Verification is the same environment-level issue already tracked via ticket 06 - not repeated as a new follow-up here.
