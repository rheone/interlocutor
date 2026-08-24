# 07 - Interlocutor.EntityFrameworkCore test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.EntityFrameworkCore.Tests
- [x] Build passes
- [x] Full test run passes

## Scope

- src project: `src/Interlocutor.EntityFrameworkCore` — one production type in scope: `TransactionBehavior<TRequest, TResponse, TContext>` (`src/Interlocutor.EntityFrameworkCore/TransactionBehavior.cs`).
- test project: `tests/Interlocutor.EntityFrameworkCore.Tests` — two test files, both already had a matching test file for the one production class (no missing-test-file gap).
- Test framework: xUnit v3. Mocking library: NSubstitute (referenced in the csproj, previously unused by these tests — now used once, see below). `TreatWarningsAsErrors` is off for this repo (commented out in `Directory.Build.props`), so any ambiguity annotation would use `#warning`; none were needed.

## Infrastructure note

`tests/Interlocutor.EntityFrameworkCore.Tests/TransactionBehaviorCommitRollbackTests.cs` runs against a real, open `Microsoft.Data.Sqlite` in-memory connection (`DataSource=:memory:`), not EF Core's InMemory provider or a mock `DbContext`. This is deliberate and documented in the class's XML summary: the commit/rollback paths need a real ADO.NET transaction to observe actual commit/rollback effects (EF Core's InMemory provider doesn't model transactions faithfully). This is real infrastructure but self-contained (in-process Sqlite, no external DB/network/filesystem dependency, no CI setup required) — flagging per the task instructions rather than treating it as a blocker.

## Discovery phase findings

- **Duplicate test IDs**: none — `dotnet exec ... --list-tests` showed 5 unique display names pre-sweep (6 post-sweep), no duplicates.
- **Skipped-test audit**: none — no `[Fact(Skip` / `[Theory(Skip` anywhere in the project.
- **Assertion anti-pattern audit**: none — no `Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.
- **Async-void audit**: none — no `async void` test methods.
- **Theory-serializer audit**: not applicable — no `[Theory]`/`TheoryData` usage in this project.
- **InternalsVisibleTo check**: not applicable — `TransactionBehavior` and its members under test are all `public`; no `internal` surface needing `InternalsVisibleTo`.
- **Missing-test-file audit**: none — the sole production class (`TransactionBehavior.cs`) has matching coverage split across `TransactionBehaviorTests.cs` (constraint/opt-out behavior) and `TransactionBehaviorCommitRollbackTests.cs` (commit/rollback/persistence behavior).
- **Project hygiene**: the test csproj does not set `<IsPackable>false</IsPackable>` explicitly, so it inherits `<IsPackable>true</IsPackable>` from the repo-wide `Directory.Build.props`. This is a solution-wide pattern — no test project in the repo overrides it — so it was flagged but **not fixed** here; fixing it only for this project would be inconsistent with every sibling test project, and a repo-wide change is out of scope for a single-package sweep.
- **Duplicate-test-pattern candidates**: none — the three commit/rollback tests differ by genuine scenario (success/commit, classified-failure/rollback, exception/rollback-and-rethrow), not by literal values only, so no parameterization opportunity.
- **`[Obsolete]` members**: none exist in `TransactionBehavior.cs` or its dependencies, so no `#pragma warning disable CS0618` wrapping was needed.

## Changes made

1. **`tests/Interlocutor.EntityFrameworkCore.Tests/TransactionBehaviorTests.cs`** — retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Act & Assert` where one statement covers both) section comments onto both existing test methods (`ExternalStoreRequest_SkipsTransaction_EvenThoughAlsoTransactional`, `TRequest_Constraint_RejectsNonTransactionalRequestAtCompileTime`). No coverage gaps or other quality violations found in this file.
2. **`tests/Interlocutor.EntityFrameworkCore.Tests/TransactionBehaviorCommitRollbackTests.cs`**:
   - Retrofitted AAA section comments onto all three existing tests (`ClassifiedSuccess_CommitsTransaction_AndPersistsViaSaveChanges`, `ClassifiedFailure_RollsBack_WithoutSecondSaveChangesCall_AndReturnsResultUnchanged`, `SaveChangesThrowing_RollsBackTransaction_AndRethrowsOriginalException`).
   - **Coverage gap filled**: added `Handle_UsesInjectedClassifier_NotOutcomeShape_ToDecideCommitVersusRollback`, tagged `// Auto Generated, verify expected behavior:`. All three pre-existing tests only exercised the *default* `IOutcomeClassifier`, whose `IsFailure` decision happens to align with the `OperationOutcome`'s own Success/Failure case shape — none proved the behavior actually delegates to the *injected* classifier rather than inspecting the outcome shape itself. The sibling `Interlocutor.NHibernate.Tests.TransactionBehaviorTests` already has this exact coverage (`Handle_RollsBackSession_WhenClassifierReportsFailure`) and its comments explicitly document that both `TransactionBehavior` implementations share the same `!this.classifier.IsFailure(result)` decision expression — this test brings the EF Core suite to parity. The new test stubs `IOutcomeClassifier` (via NSubstitute, added as a `using`) to report failure for a result that is itself `Success`-shaped, and asserts a rollback (no persisted row) occurs despite `result.IsSuccess` being `true` — proving classifier authority over outcome shape.
   - Added `using Interlocutor.Abstractions.Pipeline.Classifiers;` and `using NSubstitute;` to support the new test.

## Ambiguities / annotations left in code

None. `TreatWarningsAsErrors` is off repo-wide, but no ambiguous or questionable existing behavior was found that warranted a `#warning` or `// SWEEP-AMBIGUITY:` annotation.

## Verification

- Per-class: `dotnet build tests/Interlocutor.EntityFrameworkCore.Tests/...csproj` (0 warnings, 0 errors), followed by `dotnet exec ...Tests.dll --filter-class "*TransactionBehaviorTests"` (2/2 passed) and `--filter-class "*TransactionBehaviorCommitRollbackTests"` (4/4 passed, confirming the new test ran and passed).
- Final: full `dotnet build` of the test project (0 warnings, 0 errors) and full `dotnet exec ...Tests.dll` run — 6/6 passed, 0 failed, 0 skipped.
- Note: `dotnet test` against this project fails in this environment with a Microsoft.Testing.Platform/VSTest-bridge error ("Testing with VSTest target is no longer supported... opt-in to the new dotnet test experience"), unrelated to the sweep. Worked around by building then invoking the built test binary directly (`dotnet exec .../Interlocutor.EntityFrameworkCore.Tests.dll`), which is the underlying Microsoft.Testing.Platform executable and produces the same pass/fail results `dotnet test` would report.

## Follow-ups

None outstanding — see `## Follow-ups` note only if something is left undone; nothing is. The `IsPackable` repo-wide hygiene item above is a solution-wide, pre-existing pattern (not introduced or worsened by this sweep) and is noted for awareness only, not as unresolved work from this ticket.
