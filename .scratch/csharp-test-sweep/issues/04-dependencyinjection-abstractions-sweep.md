# 04 - Interlocutor.DependencyInjection.Abstractions test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.DependencyInjection.Abstractions.Tests
- [x] Build passes
- [x] Full test run passes

## Discovery findings

- **Test framework / mocking library:** xUnit v3 (`xunit.v3`, in-process console runner) + NSubstitute. No companion-skill gaps.
- **Duplicate test IDs:** none - `--list-tests` produced 95 unique display names before the sweep (111 after adding coverage), no duplicates.
- **Skipped tests:** none found (`[Fact(Skip`/`[Theory(Skip` grep was empty).
- **Assertion anti-patterns:** none found (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)` etc. grep was empty).
- **`async void` tests:** none found.
- **`TheoryData<T>` / serializer audit:** no `TheoryData<T>` usage; the only `[Theory]`/`[InlineData]` pairs use the `RegistrationLifetime` enum, which needs no custom xUnit serializer.
- **InternalsVisibleTo:** no `[assembly: InternalsVisibleTo]` exists for this src/test pair, and none is needed - the project's `internal` members (`HandlerDiscovery.IsCandidateType`/`DescribeHandler`, `RegistrationValidator`, `InterlocutorPipelineBuilder`'s internal constructor/`SetDefault`) are referenced only in XML doc `<see cref>` comments, never called directly from the test assembly; every test exercises them through the public `InterlocutorBuilder`/`InterlocutorPipelineBuilder` surface.
- **Test project hygiene:** `Interlocutor.DependencyInjection.Abstractions.Tests.csproj` looks correct - `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.Testing.Platform`, `NSubstitute` + `NSubstitute.Analyzers.CSharp`, `coverlet.collector` all present; `IsPackable`/`TreatWarningsAsErrors` are inherited from the shared `Directory.Build.props` (packable defaults to true solution-wide but test projects aren't published; no per-project override needed). No issues flagged.
- **Missing test files (coverage gap):** four public types in `src/Interlocutor.DependencyInjection.Abstractions` had no dedicated test coverage and were not exercised by any existing test's assertions:
  - `InterlocutorGeneratedHandlerAttribute` - constructor property assignment + null-guard on `requestType` (untested; `responseType` is legitimately nullable for void-command handlers).
  - `InterlocutorGeneratedModuleAttribute` - constructor property assignment + null-guard on `moduleType`.
  - `InterlocutorRegistrationValidationException` - `Report` assignment, `BuildMessage`'s problem-list formatting, and its own null-guard on `report`. This type is thrown by the container-specific translator packages (Microsoft/Autofac), not by anything in this project, but it's a public type this assembly owns.
  - `RegistrationReport` - exercised indirectly through `InterlocutorBuilderTests` (`DescribeRegistrations`, `DescribeRegistrations_Format_ProducesNonEmptyReadableText`) but its own constructor null-guards, `ToString()`/`Format()` delegation, empty-section omission, and open-generic type-name rendering (the `<,>` arity marker) were untested in isolation.

  All other src types with no test file are simple data shapes the sweep's skip conventions cover without a dedicated file: enums (`GeneratedHandlerKind`, `RegistrationKind`, `RegistrationLifetime`, `RegistrationProblemKind`, `RegistrationProvenance`, `TypeMatchTarget`), records/record structs (`InterlocutorFactoryRegistration`, `InterlocutorRegistration`, `RegistrationProblem`, `SkippedRegistration`, `TypeMatchRule`), and the `IRootScopeDetector` interface (exercised via `RootScopeGuardedMediatorTests`' `NSubstitute` double).
- **Duplicate-test-pattern candidates:** none beyond the two already-parameterized `[Theory]` methods in `RegistrationApplierTests` (`Apply_ForwardsEachRegistrationsLifetime_Unchanged`, `Apply_ForwardsEachFactoryRegistrationsLifetime_Unchanged`), which already use `[InlineData]` correctly.
- **Real infrastructure:** none of this project's tests touch a database, filesystem, or network - everything runs in-memory against `InterlocutorBuilder`/`InterlocutorPipelineBuilder`/`RegistrationApplier`/`RegistrationValidator`/`RootScopeGuardedMediator` plus NSubstitute doubles.

## Files touched

- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/HandlerDiscoveryTests.cs` - AAA comments retrofitted onto all 8 existing tests (no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorBuilderTests.cs` - AAA comments retrofitted onto all 34 existing tests (no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorPipelineBuilderTests.cs` - AAA comments retrofitted onto all 30 existing tests (no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/RegistrationApplierTests.cs` - AAA comments retrofitted onto all 3 existing test methods (2 are `[Theory]` with 3 `[InlineData]` cases each; no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/RegistrationValidatorTests.cs` - AAA comments retrofitted onto all 8 existing tests (no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/RootScopeGuardedMediatorTests.cs` - AAA comments retrofitted onto all 8 existing tests (no logic changes).
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorGeneratedHandlerAttributeTests.cs` - **new**, 3 tests, tagged `// Auto Generated, verify expected behavior:`.
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorGeneratedModuleAttributeTests.cs` - **new**, 2 tests, tagged `// Auto Generated, verify expected behavior:`.
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorRegistrationValidationExceptionTests.cs` - **new**, 3 tests, tagged `// Auto Generated, verify expected behavior:`.
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/RegistrationReportTests.cs` - **new**, 8 tests, tagged `// Auto Generated, verify expected behavior:`.

No changes were made to `src/Interlocutor.DependencyInjection.Abstractions` - this sweep is test-only.

## Coverage gaps filled (Auto Generated, verify expected behavior)

- `InterlocutorGeneratedHandlerAttribute`: constructor assigns `RequestType`/`ResponseType`/`Kind`; `ResponseType` accepts `null` (void-command shape); `RequestType` throws `ArgumentNullException` when `null`.
- `InterlocutorGeneratedModuleAttribute`: constructor assigns `ModuleType`; throws `ArgumentNullException` when `null`.
- `InterlocutorRegistrationValidationException`: constructor assigns `Report`; `Message` aggregates every `RegistrationProblem`'s kind and description; throws `ArgumentNullException` when `report` is `null`.
- `RegistrationReport`: each constructor parameter (`registrations`, `stageFactories`, `skipped`, `problems`) throws `ArgumentNullException` when `null`; `ToString()` delegates to `Format()`; `Format()` omits the stage-factories/skipped/problems sections when they're empty; `Format()` renders open-generic service/implementation types with the `<,>` arity marker; `Format()` includes `MatchedByRule` in the provenance text when present.

These are tagged `// Auto Generated, verify expected behavior:` per the sweep's upfront configuration - they assert the code's current, actual behavior (including the null-guard `ArgumentNullException`s, which weren't previously exercised), not independently re-derived intended behavior. A human should confirm these guard behaviors and the exact message/format text are the intended contract before treating them as a compatibility surface.

## Quality fixes applied to existing tests

- Retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Arrange & Act` where one statement covers both, or `// Act & Assert` where the assertion helper itself performs the act, e.g. `Assert.ThrowsAsync`) section comments onto every one of the 94 pre-existing test methods across all six original test files, per the sweep's quality checklist. No other changes were needed - no assertion anti-patterns, no `async void`, no skipped tests, no duplicate test IDs, no raw `object[]` theory data, no shared mutable static state, no `Thread.Sleep`/`Task.Delay`/`.Result`/`.GetAwaiter().GetResult()`, no non-deterministic inputs (`DateTime.Now`, unseeded `Random`, `Guid.NewGuid()`), and no mocking of the class under test.

## `[Obsolete]` members

None exist in `src/Interlocutor.DependencyInjection.Abstractions` - nothing to wrap in `#pragma warning disable/restore CS0618`.

## Ambiguity annotations

None left - `TreatWarningsAsErrors` is off solution-wide but no ambiguous-behavior findings required a `#warning`/`// SWEEP-AMBIGUITY:` annotation during this sweep.

## Anything flagged but not fixed

- Nothing was flagged and left unfixed. The two `[Theory]` methods in `RegistrationApplierTests` were already correctly parameterized (3 `RegistrationLifetime` cases each) and needed no change beyond the AAA comment retrofit.

## Follow-ups

None. All discovery findings were resolved within this sweep; the project needs no real-infrastructure (DB/network/filesystem) test setup, since every test runs against in-memory builder state and NSubstitute doubles.
