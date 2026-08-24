# 05 - Interlocutor.DependencyInjection.Autofac test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Discovery findings

- Test framework: xUnit v3 (`xunit.v3`). Mocking library: NSubstitute (package referenced, but unused in this
  project - every test here builds a real Autofac `ContainerBuilder`/`IContainer` rather than mocking).
- 8 test classes, 36 `[Fact]` methods total, zero `[Theory]`/`TheoryData` usage (no theory-serializer audit
  needed).
- Duplicate test IDs: none (`--list-tests` produced 36 unique display names, matching the 36 `[Fact]` count).
- Skipped tests: none (`[Fact(Skip` / `[Theory(Skip` grep was empty).
- `async void` tests: none.
- Assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.): none found.
- `Substitute.For` / mock-the-class-under-test pattern: none - not applicable, no mocking used.
- `InternalsVisibleTo`: not configured for this test project, and not needed - the one `internal` production
  type (`AutofacRootScopeDetector`) is exercised indirectly through the public `AddInterlocutor()`/`IMediator`
  surface in `RootScopeGuardTests` and `ContainerBuilderExtensionsTests`, consistent with the public-API-first
  testing style already used throughout this project.
- Missing test files: none. All three production types
  (`InterlocutorAutofacExtensions`/`ContainerBuilderExtensions.cs`, `PersistenceContainerBuilderExtensions`,
  `AutofacRootScopeDetector`) have direct or indirect coverage.
- Project hygiene: `IsPackable` is not overridden to `false` in this test project's `.csproj` — it inherits
  `true` from the root `Directory.Build.props`. This is a repo-wide pattern (confirmed the same is true of
  `Interlocutor.CommonKernel.Tests`), not unique to this project, so left unchanged rather than fixed in
  isolation here (see Follow-ups).
- Duplicate-test-pattern candidates for parameterization: none. The near-identical-looking triples (e.g.
  `AddCancellationBehavior_ResolvesWithoutThrowing_UnderAutofac` /
  `AddTimeoutBehavior_ResolvesWithoutThrowing_UnderAutofac` / `AddRateLimitedBehavior_ResolvesWithoutThrowing_UnderAutofac`)
  differ by request type, response type, and marker interface, not just literal values, so collapsing them into
  one `[Theory]` would obscure rather than clarify.
- Real infrastructure: `PersistenceRegistrationTests` uses a real EF Core `SqliteConnection` against
  `DataSource=:memory:` (via `Microsoft.EntityFrameworkCore.Sqlite`) to prove `IUnitOfWork`/`IDomainRepository`
  registrations share one scoped `DbContext`. This is in-memory only (no on-disk file, no network) and already
  present before this sweep - left as-is, flagged here per the task's instruction to note any real-infrastructure
  dependency rather than block on it.

## Sweep changes

Applied to all 8 files (every existing test method touched, so AAA comments were retrofitted everywhere the
skill requires it):

- `BehaviorSubsetConstraintTests.cs` - retrofitted `// Arrange` / `// Act` / `// Assert` on all 7 tests.
- `ContainerBuilderExtensionsTests.cs` - retrofitted AAA on all 4 existing tests; added 1 new coverage-gap test.
- `GeneratedDispatchSwitchIntegrationTests.cs` - retrofitted AAA on all 4 tests.
- `GeneratedRegistrationIntegrationTests.cs` - retrofitted AAA on its 1 test.
- `InterlocutorBuilderIntegrationTests.cs` - retrofitted AAA on all 15 tests.
- `NotificationFanOutIntegrationTests.cs` - retrofitted AAA on its 1 test.
- `PersistenceRegistrationTests.cs` - retrofitted AAA on both existing tests; added 2 new coverage-gap tests.
- `RootScopeGuardTests.cs` - retrofitted AAA on both tests.

### Coverage gaps filled (tagged `// Auto Generated, verify expected behavior:`)

`InterlocutorAutofacExtensions.AddInterlocutor` and `PersistenceContainerBuilderExtensions`'s two methods each
guard their delegate parameter with `ArgumentNullException.ThrowIfNull(...)`, but no test exercised the null
path. Added:

- `ContainerBuilderExtensionsTests.AddInterlocutor_NullConfigure_ThrowsArgumentNullException`
- `PersistenceRegistrationTests.AddInterlocutorUnitOfWork_NullFactory_ThrowsArgumentNullException`
- `PersistenceRegistrationTests.AddInterlocutorRepository_NullFactory_ThrowsArgumentNullException`

Each asserts both the exception type and `ParamName`, reflecting the guard clause as written today.

### Quality fixes applied to existing tests

- AAA section comments retrofitted on every one of the 36 pre-existing tests (see file list above); a few
  tests that split "act" across two statements (e.g. build the container, then resolve from it) got their
  `// Act` placed to cover both, matching how the rest of the surrounding test bodies already read.
- No other quality violations found - assertions were already scoped correctly, no vacuous tests, no
  shared mutable static state, no `Thread.Sleep`/`Task.Delay`, no blocking `.Result`/`.Wait()`, no
  non-deterministic values used unseeded.
- `[Obsolete]` members: none exist in `Interlocutor.DependencyInjection.Autofac`, so no `#pragma warning
  disable/restore CS0618` wrapping was needed.

### Ambiguities / annotations left in place

None. No `#warning` or `// SWEEP-AMBIGUITY:` comments were needed - the existing test suite was already
comprehensive and consistent with the sweep's quality checklist before this pass; the only real gap was the
three null-guard tests listed above.

## Verification

- [x] Sweep applied to `tests/Interlocutor.DependencyInjection.Autofac.Tests` (all 8 classes)
- [x] `dotnet build tests/Interlocutor.DependencyInjection.Autofac.Tests/Interlocutor.DependencyInjection.Autofac.Tests.csproj` passes (0 errors, only pre-existing warnings unrelated to this sweep: `SA1201` x2, `INTERLOC001` x1, none introduced by this change)
- [x] Full test run passes: 39/39 (36 original + 3 new), 0 failed, 0 skipped

## Follow-ups

- `IsPackable=false` is missing from this test project's `.csproj` (it inherits `IsPackable=true` from the root
  `Directory.Build.props`). This is a repo-wide gap, not specific to this package - fixing it here alone would
  be inconsistent with every other test project. Worth a separate, repo-wide ticket if the maintainers want test
  assemblies excluded from packing.
