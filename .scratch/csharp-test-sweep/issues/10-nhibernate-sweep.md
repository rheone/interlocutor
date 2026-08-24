# 10 - Interlocutor.NHibernate test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Scope

- src project: `src/Interlocutor.NHibernate` (single production type: `TransactionBehavior<TRequest, TResponse>`)
- test project: `tests/Interlocutor.NHibernate.Tests` (single test class: `TransactionBehaviorTests`)
- Framework: xUnit v3 (Microsoft.Testing.Platform runner), mocking library: NSubstitute
- Target: `net11.0` only (no multi-targeting, no net4x runner concerns)

## Discovery findings

- **Test projects**: one candidate, `tests/Interlocutor.NHibernate.Tests` — matches skill scope, no selection dialog needed.
- **Duplicate test IDs**: none. `dotnet test --list-tests` fails under this SDK's `dotnet test` (`Testing with VSTest target is no longer supported... on .NET 10 SDK and later`); worked around by invoking the built `Interlocutor.NHibernate.Tests` executable directly. Ran the full suite: 11 tests total (5 `[Fact]` + 6 `[Theory]` rows from `ClassifiedOutcomes`), all passed, count matches expectations exactly — no silent collapsing from duplicate display names.
- **Skipped tests**: none (`[Fact(Skip` / `[Theory(Skip` grep returned no hits).
- **Assertion anti-patterns**: none (`Assert.True(x == ...)`, `Assert.Equal(true/false/null, ...)` etc. grep returned no hits).
- **`async void` tests**: none.
- **Theory serializer audit**: `ClassifiedOutcomes` is a `TheoryData<Func<...>, bool>` — `Func<>` is a non-primitive type with no `[assembly: RegisterXunitSerializer]` registered anywhere in the repo. In practice this didn't cause a problem (all 6 rows executed and reported individually, confirmed by the total-test-count check above), so left as-is; noted under Follow-ups in case a future xUnit version tightens this.
- **`InternalsVisibleTo`**: not applicable — `TransactionBehavior<TRequest, TResponse>` exposes no `internal` members.
- **Missing test files**: none — the sole production type already has a corresponding test class with full member coverage (constructor defaults exercised via the theory test, `Handle` exercised across all branches).
- **Project hygiene**: `tests/Interlocutor.NHibernate.Tests.csproj` has no `<IsPackable>` override, consistent with every other test project in the repo (repo-wide convention, not a gap). xUnit v3 + `Microsoft.Testing.Platform` + `NSubstitute` + analyzer packages are all present and current.
- **Duplicate-test-pattern candidates**: none — all 6 `[Fact]` methods cover distinct scenarios; the parameterized commit/rollback matrix is already a `[Theory]`.
- **`[Obsolete]` members**: none in `TransactionBehavior<TRequest, TResponse>` — nothing to wrap in `#pragma warning disable CS0618`.
- **Coverage check against `TransactionBehavior<TRequest, TResponse>`'s public surface**: constructor (default classifier/describer fallback, `IExternalStoreRequest` opt-out, `TRequest : ITransactionalRequest` compile-time constraint) and `Handle` (commit path, rollback path, exception-during-commit path, full commit/rollback matrix against every classified outcome case) are all already covered. No coverage gaps found — no new tests were added.
- **Note (not a test gap)**: the `_describer` field (from the optional `describer` constructor parameter) is assigned but never read inside `Handle` — identical to the sibling `Interlocutor.EntityFrameworkCore.TransactionBehavior<,,>`, so this is an existing production-code characteristic, not something this sweep introduced or could meaningfully add a behavioral test for (nothing observable to assert without reflecting into a private field). Left untouched — production code is out of scope for a test sweep.

## What changed

- `tests/Interlocutor.NHibernate.Tests/TransactionBehaviorTests.cs`: retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Act & Assert` where one statement covers both) section comments onto all 6 existing test methods, none of which had them before:
  - `ExternalStoreRequest_SkipsSession_EvenThoughAlsoTransactional`
  - `TRequest_Constraint_RejectsNonTransactionalRequestAtCompileTime` (two Act & Assert pairs — positive and negative constraint check)
  - `Handle_CommitsSession_WhenClassifierReportsSuccess`
  - `Handle_RollsBackSession_WhenClassifierReportsFailure`
  - `Handle_RollsBackAndRethrowsOriginalException_WhenCommitFailsDuringPersistence`
  - `Handle_CommitOrRollbackDecision_MatchesEntityFrameworkCoreBehavior_ForIdenticalClassifiedInput` (theory; existing sanity-check assertion of the shared default classifier kept as part of Arrange, since it validates test setup rather than the behavior under test)

No production code was touched. No new tests were added (no coverage gaps found). No quality violations required fixing beyond the missing AAA comments.

## Ambiguities / annotations left

None — no `#warning` or `// SWEEP-AMBIGUITY:` markers were needed; nothing in the existing test file was ambiguous about intended behavior vs. actual behavior.

## Verification

- `dotnet build tests/Interlocutor.NHibernate.Tests/Interlocutor.NHibernate.Tests.csproj` — succeeded, 0 warnings, 0 errors.
- Ran the built test executable directly (`dotnet test` itself errors under this SDK with "Testing with VSTest target is no longer supported... on .NET 10 SDK and later"; the built `.exe` in `bin/Debug/net11.0/` is the working entry point) filtered to `TransactionBehaviorTests`: 11/11 passed.
- Full test executable run (whole project, same 11 tests — this is the only test class in the project): 11/11 passed, 0 skipped, 0 failed.

## Follow-ups

- The `dotnet test`/`dotnet test --list-tests` commands documented in the skill's verification steps do not work as-is against this SDK (`.NET 11 preview` + `Microsoft.Testing.Platform`) — they fail with "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later." Running the built executable directly (`bin/Debug/{tfm}/{AssemblyName}[.exe]`) works and was used instead. Worth confirming whether this affects CI too, or whether CI already uses the new `dotnet test` experience / MTP invocation.
- No `[assembly: RegisterXunitSerializer(...)]` exists anywhere in the repo, and `ClassifiedOutcomes` uses `TheoryData<Func<...>, bool>` (a non-primitive, non-registered element type). It ran correctly here, but if a stricter xUnit v3 release starts enforcing serializer registration for `Func<>` theory data, this test would need either a registered serializer or a refactor away from `Func<>` rows (e.g. an enum discriminator mapped to outcome factories). Not fixed now since nothing is currently broken.
