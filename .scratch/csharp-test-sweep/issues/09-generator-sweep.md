# 09 - Interlocutor.Generator test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.Generator.Tests
- [x] Build passes
- [x] Full test run passes

## What was done

Swept all 8 test classes in `tests/Interlocutor.Generator.Tests` (59 `[Fact]` tests total,
`GeneratorTestHarness.cs` skipped as a `*Harness` helper per the discovery rules):

- `CrossAssemblyAggregationTests.cs` (7 tests)
- `CrossAssemblyContainerResolutionTests.cs` (2 tests)
- `FullPipelineIncrementalCachingTests.cs` (2 tests)
- `HandlerDiagnosticAnalyzerTests.cs` (3 tests)
- `HandlerDiscoveryEmissionTests.cs` (5 tests)
- `IncrementalCachingTests.cs` (1 test)
- `MissingHandlerDiagnosticTests.cs` (11 tests)
- `NotificationHandlerOrderingTests.cs` (10 tests)
- `OpenGenericHandlerTests.cs` (11 tests)
- `RequestDispatchSwitchEmissionTests.cs` (7 tests)

This is a source generator project: production code under `src/Interlocutor.Generator` is entirely
`internal` except `InterlocutorSourceGenerator` itself, so nothing is unit-tested class-by-class —
every test drives the real Roslyn `CSharpGeneratorDriver` end to end through the hand-rolled
`GeneratorTestHarness` (documented rationale: the idiomatic
`Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit` package pulls xunit v2 transitively,
which conflicts with this repo's xunit.v3-only convention) and asserts on emitted source text,
reported diagnostics, and incremental-caching step outcomes (`IncrementalStepRunReason`). AAA
mapped onto this pattern as Arrange = the source snippet(s) compiled, Act = the
`GeneratorTestHarness.Run`/`RunAndUpdate`/`RunTwiceTrackingSteps` call, Assert = the diagnostic/
generated-source/step-reason assertions.

### Coverage gaps

None filled. Every diagnostic (`INTERLOC001`–`INTERLOC007`), the open-generic-handler resolution
paths (constraint matching, closed-beats-open precedence, `HandlerOverrideAttribute` and its own
validation), notification handler ordering (including the cycle diagnostic), cross-assembly
aggregation (direct/transitive/diamond references, out-of-range `HandlerKind` rejection), the
zero/single-handler dispatch-switch emission cases, and both whole-pipeline and step-scoped
incremental-caching claims already had dedicated tests before this sweep. No missing-test-file gap
either: since only one production type is public, a 1:1 class-to-test-file audit isn't meaningful
here — coverage is organized by generator *behavior* (diagnostics, emission, caching, cross-assembly),
and every behavior ticket referenced in the file-level doc comments (tickets 10–13, 53, 54, ADR-0007,
ADR-0008) has a corresponding test class.

### Quality fixes applied

- **Assertion anti-pattern (5 occurrences, discovery step 13)**: `Assert.True(errors.Count == 0,
  string.Join(Environment.NewLine, errors.Select(e => e.ToString())))` replaced with
  `Assert.Empty(errors)` in `HandlerDiscoveryEmissionTests.cs`, `NotificationHandlerOrderingTests.cs`,
  `OpenGenericHandlerTests.cs` (2 occurrences), and `RequestDispatchSwitchEmissionTests.cs`.
- **AAA section comments** retrofitted onto every one of the 59 existing `[Fact]` methods across all
  8 files (`// Arrange`, `// Act`, `// Assert`; a couple of tests that only reference a class-level
  `const string` source constant have no local Arrange statement, so only `// Act`/`// Assert` apply
  there).

No other quality violations found: no mocking anywhere in this project (none needed — it drives a
real compiler/generator, nothing to substitute), no shared mutable state, no `Thread.Sleep`, no
magic-value repetition warranting extraction beyond the existing named `const string` source
constants, and no test mocking the class under test (not applicable — no mocking library is
referenced).

### Discovery audits completed

- **Duplicate test IDs**: built the test host and ran it with `--list-tests`; `sort | uniq -d`
  against the 59 display names — no duplicates.
- **Skipped tests**: `grep -rn "\[Fact(Skip\|\[Theory(Skip"` — no matches.
- **Assertion anti-patterns**: found and fixed, see above.
- **`async void`**: `grep -rn "async void"` — no matches (the two container-resolution tests are
  properly `async Task`).
- **Theory-data serializers**: no `[Theory]`/`TheoryData<T>` usage anywhere in the project — every
  test is a `[Fact]`. Nothing to register.
- **`InternalsVisibleTo`**: not needed — the tests never reference an `internal` member of
  `Interlocutor.Generator` directly; everything is exercised through the public
  `InterlocutorSourceGenerator` entry point via the Roslyn driver APIs.
- **Missing test files**: see Coverage gaps above — no class-level gap found; coverage is organized
  by generator behavior rather than 1:1 per internal type.
- **Project hygiene**: `Interlocutor.Generator.Tests.csproj` has no `<IsPackable>` setting, matching
  every other test project repo-wide (not a per-project defect). Package references (xunit.v3,
  xunit.runner.visualstudio, Microsoft.Testing.Platform, Microsoft.NET.Test.Sdk,
  Microsoft.CodeAnalysis.CSharp, coverlet.collector) are current, no outdated v1/v2 framework
  references.
- **Duplicate-test-pattern candidates**: no two `[Fact]` methods within any file differ only by a
  literal value in a way that would collapse cleanly into a `[Theory]` — every test compiles a
  structurally distinct source snippet and asserts on a distinct diagnostic/emission shape, so
  parameterizing would obscure rather than clarify what each case is proving. No refactor candidates.
- **`[Obsolete]` members**: none exist anywhere in `src/Interlocutor.Generator` — nothing to wrap in
  `#pragma warning disable CS0618`.
- **Real infrastructure**: none of these tests need a database, file system, or network — they
  compile in-memory `CSharpCompilation`s and (in `CrossAssemblyContainerResolutionTests`) load
  emitted PE bytes via `Assembly.Load(byte[])` entirely in-process. No infrastructure note needed.

No `#warning`/`// SWEEP-AMBIGUITY:` annotations were left — no ambiguous or contradictory behavior
was found during the sweep.

### Verification

- `dotnet build tests/Interlocutor.Generator.Tests/Interlocutor.Generator.Tests.csproj` — succeeded,
  0 warnings, 0 errors, after every file touched during the sweep.
- `dotnet test`'s VSTest bridge fails in this environment with a pre-existing, unrelated CLI/SDK
  error (`Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10
  SDK and later`) — the same environment quirk documented in ticket 03. Worked around by running the
  built Microsoft.Testing.Platform executable directly:
  `tests/Interlocutor.Generator.Tests/bin/Debug/net11.0/Interlocutor.Generator.Tests.dll` — all 59
  tests pass, 0 failed, 0 skipped, both per-class during the sweep loop and as a final full run.
- `dotnet build` (full solution) — succeeded, 0 errors; the 53 pre-existing warnings reported are all
  in unrelated test projects (`Interlocutor.Abstractions.Tests`,
  `Interlocutor.DependencyInjection.Abstractions.Tests`,
  `Interlocutor.DependencyInjection.Microsoft.Tests`) and were not introduced by this sweep.

## Follow-ups

- Same `dotnet test` VSTest-bridge environment issue already flagged in ticket 03 applies here too —
  not re-litigated, just confirmed reproducible on this project as well.
