# 12 - Interlocutor.Persistence.EntityFrameworkCore test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.Persistence.EntityFrameworkCore.Tests
- [x] Build passes
- [x] Full test run passes

## Scope

`src/Interlocutor.Persistence.EntityFrameworkCore` (`EntityFrameworkRepository<TEntity,TKey,TContext>`,
`EntityFrameworkUnitOfWork<TContext>`) and `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests`
(`EntityFrameworkRepositoryTests`, `SharedSessionIntegrationTests`, and the newly added
`EntityFrameworkUnitOfWorkTests`).

## Infrastructure note

All tests in this project (before and after the sweep) run against a real `Microsoft.Data.Sqlite`
`DataSource=:memory:` connection kept open for the test's lifetime, wired through `Microsoft.EntityFrameworkCore.Sqlite`.
This is the Sqlite-in-memory pattern, not a real on-disk/networked database, so it does not need any
external infra (no Docker, no connection string, no CI service container) - it was **not** treated as a
blocker per the task's guidance. Two of the existing tests deliberately assert on `IQueryable.ToQueryString()`
output (`WHERE`, `ORDER BY`, `LIMIT`) to prove filter/sort/paging push down to SQL rather than being applied
in memory after materialization - this is intentional and load-bearing, not incidental coupling to EF's SQL
generation.

## Discovery findings

- **Duplicate test IDs**: none (`--list-tests | sort | uniq -d` empty, both before and after the sweep).
- **Skipped tests**: none.
- **Assertion anti-patterns** (`Assert.True(x == y)` etc.): none found.
- **`async void` tests**: none found.
- **Theory/TheoryData usage**: none in this project (no `[Theory]` tests existed or were added - every
  case in scope is a distinct pass/fail scenario, not a parameterization of one behavior).
- **`InternalsVisibleTo` / internal members**: no `internal` members in the two production types; no
  `InternalsVisibleTo` needed.
- **Missing test file audit**: `EntityFrameworkUnitOfWork.cs` had no dedicated `*Tests.cs` file - its
  `SaveChangesAsync` was previously exercised only incidentally inside `EntityFrameworkRepositoryTests` and
  `SharedSessionIntegrationTests`. Closed by adding `EntityFrameworkUnitOfWorkTests.cs` (see below).
- **Project hygiene**: `Interlocutor.Persistence.EntityFrameworkCore.Tests.csproj` doesn't set
  `<IsPackable>false</IsPackable>` locally, but neither does any of the other 14 test projects in the repo -
  `IsPackable` is set `true` at the repo-root `Directory.Build.props` and no test project overrides it. This
  is an established repo-wide pattern, not a defect local to this project, so it was left as-is rather than
  fixed unilaterally in one project's csproj.
- **Duplicate-test-pattern candidates**: none - no two `[Fact]` bodies differ only by literal values.
- **`[Obsolete]` members**: none exist in `src/Interlocutor.Persistence.EntityFrameworkCore`, so the
  `#pragma warning disable/restore CS0618` instruction did not apply.
- **Environment note (not a code defect)**: `dotnet test <csproj>` fails in this sandbox with
  `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`
  for every project in the solution (reproduced against `Interlocutor.CommonKernel.Tests` too), so it's an
  environment/SDK configuration issue, not something introduced by this sweep. Verification instead used
  `dotnet build` followed by `dotnet exec <test.dll>` / `dotnet exec <test.dll> --list-tests`, which the
  Microsoft.Testing.Platform apphost supports directly and which surfaced the same pass/fail/list-tests
  information `dotnet test` would have.

## Changes made

### `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/EntityFrameworkRepositoryTests.cs`

- Retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Arrange & Act`) section comments onto all 5
  pre-existing tests.
- Filled coverage gaps (tagged `// Auto Generated, verify expected behavior:`):
  - `GetManyAsync_WithNoFilterOrSort_ReturnsEveryEntity` - the `filter`/`sort` optional-parameter default
    path (`null`/`null`) was untested; only the filtered+sorted path had a test.
  - `GetPagedAsync_WithFilter_AppliesFilterToBothCountAndItemsQueries` - `GetPagedAsync`'s `filter` parameter
    was untested; only its `sort`/paging parameters had a test.
  - `IdSelector_ReturnsExpressionSuppliedAtConstruction` - the `IdSelector` property (required by
    `IDomainRepository<TEntity,TKey>.GetById`'s default interface method) had no direct test.
  - `GetById_ReturnsMatchingEntity_WhenQueryTranslatesThroughEfCore` and
    `GetById_ReturnsNull_WhenNoEntityHasThatKey` - `GetById` is a default interface method built on `Query`
    and `IdSelector`; `Interlocutor.Persistence.Abstractions.Tests` already covers its logic against a stub
    `IQueryable`, but nothing in this package proved the expression tree it builds
    (`Expression.Equal(idSelector.Body, Expression.Constant(id))`) actually translates through EF Core's real
    SQL provider. Both new tests call it via an explicit `IDomainRepository<Widget,int>` cast, since default
    interface methods aren't reachable through the concrete type.

### `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/SharedSessionIntegrationTests.cs`

- Retrofitted `// Arrange` / `// Act` / `// Assert` section comments onto both pre-existing tests. No
  coverage gaps or quality violations found in this class.

### `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/EntityFrameworkUnitOfWorkTests.cs` (new file)

Closes the missing-test-file gap for `EntityFrameworkUnitOfWork<TContext>` with two tests
(tagged `// Auto Generated, verify expected behavior:` at the class level), each with `// Arrange` /
`// Act` / `// Assert` comments:

- `SaveChangesAsync_ForwardsToContext_PersistingPendingChanges` - stages a change directly against the
  `DbContext` (not through the repository, to isolate the unit-of-work behavior) and confirms
  `SaveChangesAsync` persists it.
- `SaveChangesAsync_WithNoPendingChanges_CompletesWithoutPersistingAnything` - calls `SaveChangesAsync` with
  nothing staged and asserts the table remains empty (an assertion-free "doesn't throw" test would have
  violated the "every test has at least one assertion" rule).

## Ambiguities / annotations left in code

None - no `// SWEEP-AMBIGUITY:` or `#warning` markers were needed; nothing in this project's behavior was
unclear relative to its documented contract.

## Verification

- `dotnet build tests/Interlocutor.Persistence.EntityFrameworkCore.Tests/Interlocutor.Persistence.EntityFrameworkCore.Tests.csproj`
  - 0 errors, 0 warnings, after each class was touched and again at the end.
- `dotnet exec .../Interlocutor.Persistence.EntityFrameworkCore.Tests.dll --list-tests | sort | uniq -d`
  - empty (no duplicate test IDs) both before and after the sweep.
- `dotnet exec .../Interlocutor.Persistence.EntityFrameworkCore.Tests.dll`
  - before: 7/7 passed. After: **14/14 passed**, 0 failed, 0 skipped.
- `dotnet build Interlocutor.slnx` (full solution) - 0 errors (60 pre-existing warnings across other
  projects, none introduced by this sweep).

## Follow-ups

None required for this ticket - all discovery findings were either non-issues (clean audits) or were
resolved directly (missing test file). The `IsPackable`/project-hygiene note above is informational only:
fixing it would mean a repo-wide `Directory.Build.props` or per-project decision affecting all 14 test
projects, which is out of scope for a single-package sweep.
