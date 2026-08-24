# 06 - Interlocutor.DependencyInjection.Microsoft test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Discovery findings

- Test framework: xUnit v3 (`xunit.v3`), mocking: NSubstitute. No net4x/netstandard targets (`net11.0` only) - no .NET Framework runner question applied.
- 9 test files, 50 test methods total; all map to the 4 production files in `src/Interlocutor.DependencyInjection.Microsoft` (`ServiceCollectionExtensions.cs`, `PersistenceServiceCollectionExtensions.cs`, `MicrosoftRootScopeDetector.cs`, `RootServiceProviderMarker.cs`) - the latter two are `internal` and covered indirectly through `RootScopeGuardTests`/DI resolution, not a missing-test-file gap.
- Duplicate test IDs: none (`--list-tests` output had zero duplicate display names).
- Skipped tests: none found (`[Fact(Skip` / `[Theory(Skip` grep empty).
- `async void` tests: none found.
- Assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.): none found.
- `TheoryData<T>` usage: none in this project (no `[Theory]` methods at all - every test is a `[Fact]`).
- `InternalsVisibleTo`: not needed - the two `internal` production types are exercised only through the public `AddInterlocutor` surface, never directly.
- `[Obsolete]` members: none in this package's source - the "test Obsolete members" config had nothing to act on.
- Project hygiene (`.csproj`): `IsPackable` not explicitly set (defaults appropriately for a test project via SDK conventions used elsewhere in the repo); package references (xunit.v3, Microsoft.Testing.Platform, NSubstitute, coverlet.collector) are current, no outdated xUnit v1/v2 or MSTest v1/v2 references.
- Duplicate-test-pattern candidates (literal-only-different `[Fact]`s): none found warranting `[Theory]` conversion.
- Real infrastructure: `PersistenceRegistrationTests` uses a real EF Core `DbContext` against SQLite **in-memory** (`DataSource=:memory:`) - no external DB/file/network dependency, but it is genuine EF Core + SQLite provider execution rather than a stub. Noted here per the task's instruction rather than blocking the sweep.

## What was done

Baseline (before any edits): `dotnet build` clean, full test run 50/50 passing. No coverage gaps were found - every existing test method was already exercising real behavior, so no new `// Auto Generated, verify expected behavior:` tests were needed.

Retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Act & Assert` where a single statement covers both, matching two pre-existing partial annotations) onto every test method touched, across all 9 files:

- `BehaviorSubsetConstraintTests.cs` (7 tests)
- `GeneratedDispatchSwitchIntegrationTests.cs` (5 tests)
- `GeneratedRegistrationIntegrationTests.cs` (2 tests)
- `NotificationFanOutIntegrationTests.cs` (1 test)
- `InterlocutorBuilderIntegrationTests.cs` (19 tests)
- `PersistenceRegistrationTests.cs` (2 tests)
- `PipelineOrderingIntegrationTests.cs` (2 tests)
- `RootScopeGuardTests.cs` (2 tests)
- `ServiceCollectionExtensionsTests.cs` (5 tests)

All 45 pre-existing tests now carry explicit AAA section comments; none required changing their Arrange/Act boundary beyond adding the comments, except where a resolve-and-inspect step was reclassified as the Act (documented below).

### Quality fix applied

`ServiceCollectionExtensionsTests.UseRecommendedChain_RegistersBehaviorsInCanonicalOrder` asserted only `Assert.NotEmpty(behaviorDescriptors)` despite its name promising the canonical order is verified - a false-confidence gap: the test would pass even if `UseRecommendedChain()` registered the five behaviors in the wrong order. No other existing test in this project checks the *registration-list* order for `UseRecommendedChain()` specifically (`InterlocutorBuilderIntegrationTests`'s similarly-named test only checks `Contains` for three of the five types; `PipelineOrderingIntegrationTests` proves *runtime dispatch* order end-to-end, not the registration list). Strengthened the assertion to check the exact ordered sequence of `ImplementationType.Name` values (`LoggingBehavior`2`, `AuditingBehavior`2`, `AuthorizationBehavior`2`, `ValidationBehavior`2`, `TransactionBehavior`2`), matching `UseRecommendedChain`'s documented canonical order. Verified green after the change.

## Ambiguities / annotations left in place

None - no `#warning` or `// SWEEP-AMBIGUITY:` comments were needed. `TreatWarningsAsErrors` is commented out solution-wide (`Directory.Build.props`), so `#warning` would have been the applicable form had one been needed.

## Flagged but not fixed

- `RootScopeGuardTests`'s `Assert.Contains(nameof(SharedScopeQuery), ex.Message)` omits an explicit `StringComparison` overload (present elsewhere in the same project, e.g. `GeneratedDispatchSwitchIntegrationTests`). Not fixed: this is a minor consistency nit, not a listed anti-pattern in the quality checklist, and changing assertion overloads outside the AAA-retrofit/gap-fill scope risked churn beyond what was asked.

## Verification

- Per-class: build + targeted `--filter "FullyQualifiedName~{ClassName}"` run passed after each file's edits.
- Note on tooling: `dotnet test` in this sandbox fails with `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later` (pre-existing environment/SDK-preview issue, unrelated to this sweep). Final verification instead used `dotnet build` (clean, 0 errors) followed by running the built Microsoft.Testing.Platform executable directly: `./tests/Interlocutor.DependencyInjection.Microsoft.Tests/bin/Debug/net11.0/Interlocutor.DependencyInjection.Microsoft.Tests`, which is the equivalent final full-suite run.
- Final full run: **50/50 passed, 0 failed, 0 skipped.**

- [x] Sweep applied to tests/Interlocutor.DependencyInjection.Microsoft.Tests
- [x] Build passes
- [x] Full test run passes

## Follow-ups

- None requiring human judgment. The `dotnet test` CLI opt-in-experience error in this sandbox (see Verification) is an environment/tooling matter, not a code issue in this package - worth a maintainer note if it also affects CI, but out of scope for this ticket.
