# 15 - Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Discovery findings

- Test framework: xUnit v3 (`xunit.v3`), mocking: NSubstitute (referenced via `NSubstitute` / `NSubstitute.Analyzers.CSharp`, though unused in this project's single test file - no substitute/mock objects are created; both tests exercise real `ServiceCollection`/`ServiceProvider`/EF Core Sqlite behavior). `net11.0` only - no .NET Framework runner question applied.
- 1 test file (`TransactionBehaviorArityTests.cs`), 2 test methods. Per the file's own XML doc comment, this project is deliberately isolated from `Interlocutor.DependencyInjection.Microsoft.Tests` - it exists solely to prove the Microsoft-DI-specific EF Core arity failure (and its documented workaround) for `AddTransactionBehavior()` without disturbing that sibling project's NHibernate-arity-2 assumptions. Both public members exercised (`AddTransactionBehavior()`'s failure path and the documented subclass workaround) have direct test coverage - no missing-test-file gap.
- Duplicate test IDs: none (`--list-tests` output had zero duplicate display names; only 2 tests total).
- Skipped tests: none found (`[Fact(Skip` / `[Theory(Skip` grep empty).
- `async void` tests: none found - one `[Fact] void` test (synchronous throw assertion) and one `[Fact] async Task` test.
- Assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.): none found.
- `TheoryData<T>` usage: none in this project (no `[Theory]` methods - both tests are `[Fact]`, appropriately: one asserts a single fixed failure mode, the other a single fixed workaround path; neither has a natural parameterization axis).
- `InternalsVisibleTo`: not needed - all types under test (`AddTransactionBehavior`, `TransactionBehavior<,,>`, `InterlocutorPipelineBuilder.AddBehavior`) are public API, exercised through the public `AddInterlocutor`/`IMediator.Send` surface.
- `[Obsolete]` members: none in this package's source - nothing to act on.
- Project hygiene (`.csproj`): `IsPackable` not explicitly set (consistent with sibling test projects); package references (xunit.v3, xunit.runner.visualstudio, Microsoft.Testing.Platform, Microsoft.NET.Test.Sdk, coverlet.collector, NSubstitute, NSubstitute.Analyzers.CSharp, Microsoft.EntityFrameworkCore.Sqlite) are current, no outdated xUnit v1/v2 or MSTest v1/v2 references.
- Duplicate-test-pattern candidates (literal-only-different `[Fact]`s): none - only 2 tests, each covering a structurally distinct scenario (failure path vs. success-through-workaround path).
- Real infrastructure: `DocumentedSubclassWorkaround_ResolvesAndDispatches_ThroughRealServiceProvider` uses a real EF Core `DbContext` against SQLite **in-memory** (`DataSource=:memory:`) - no external DB/file/network dependency, but genuine EF Core + SQLite provider execution (same pattern already noted for ticket 06's sibling project). Noted here per the task's instruction rather than blocking the sweep.

## What was done

Baseline (before any edits): `dotnet build` clean (0 errors, 0 warnings), full test run 2/2 passing. No coverage gaps were found - both tests already exercise real behavior end-to-end (a synchronous eager-failure assertion and a full DI-resolve-and-dispatch-through-a-real-provider assertion), so no new `// Auto Generated, verify expected behavior:` tests were needed.

No changes were required: both test methods already carry full `// Arrange` / `// Act & Assert` (test 1) and `// Arrange` / `// Act` / `// Assert` (test 2) section comments, correctly placed, and no quality-checklist violation was found in either. This is the smallest project in the sweep set and was already in a clean state - the sweep confirms rather than fixes it.

## Ambiguities / annotations left in place

None - no `#warning` or `// SWEEP-AMBIGUITY:` comments were needed.

## Flagged but not fixed

None found.

## Verification

- Baseline: `dotnet build tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests/...csproj` - 0 errors, 0 warnings; full run via the built executable - **2/2 passed, 0 failed, 0 skipped** (1.18s).
- No edits were made, so no post-edit re-verification was needed beyond the baseline run above.
- Note on tooling: `dotnet test` in this sandbox fails with `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later` (same pre-existing environment/SDK-preview issue noted in ticket 06, unrelated to this sweep). Verification instead used `dotnet build` followed by running the built Microsoft.Testing.Platform executable directly.

- [x] Sweep applied to tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests
- [x] Build passes
- [x] Full test run passes

## Follow-ups

None requiring human judgment. The `dotnet test` CLI tooling gap noted in Verification is the same environment-level issue already tracked via ticket 06 - not repeated as a new follow-up here.
