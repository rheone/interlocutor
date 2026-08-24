# 13 - Interlocutor.Persistence.NHibernate test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Discovery findings

- Test project: `tests/Interlocutor.Persistence.NHibernate.Tests` (xUnit v3 + NSubstitute), covering
  `src/Interlocutor.Persistence.NHibernate` (`NHibernateRepository<TEntity, TKey>`, `NHibernateUnitOfWork`).
- 3 test classes, 7 test methods total. No duplicate test IDs (`--list-tests` confirmed 7 unique display names).
- No skipped tests (`[Fact(Skip` / `[Theory(Skip`) found.
- No `[Theory]`/`TheoryData` usage in this project — no serializer registration to audit.
- No `async void` test methods found.
- No assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true, ...)`, etc.) found.
- No `internal` members in the src project and no `InternalsVisibleTo` — nothing to check there.
- No `[Obsolete]`-marked members in the src project, so the "wrap in `#pragma warning disable CS0618`"
  instruction had nothing to apply to.
- No missing test files — both `NHibernateRepository<TEntity, TKey>` and `NHibernateUnitOfWork` have a
  matching test class.
- Project hygiene: `<IsPackable>` is not set per-project (inherits root `Directory.Build.props`'s
  `IsPackable=true`); this matches every other test project in the repo (none override it), so left as-is —
  not a defect specific to this project.
- Duplicate-test-pattern candidates: `InsertAsync_...`, `UpdateAsync_...`, `DeleteAsync_...` in
  `NHibernateRepositoryTests` are structurally similar but each asserts a different `ISession` method
  (`SaveAsync`/`UpdateAsync`/`DeleteAsync`) — not a genuine parameterization candidate (would require passing
  the verified member as theory data, hurting readability for no real duplication reduction). Left
  unparameterized, consistent with the equivalent EF Core sibling test project's style.
- Baseline `dotnet test` via the VSTest bridge fails in this environment
  (`Testing with VSTest target is no longer supported...`) because the project sets
  `UseMicrosoftTestingPlatformRunner=true` (repo-wide, in `Directory.Build.targets`). Ran tests instead by
  building and executing the produced test host directly:
  `dotnet build tests/Interlocutor.Persistence.NHibernate.Tests/Interlocutor.Persistence.NHibernate.Tests.csproj`
  then `./tests/Interlocutor.Persistence.NHibernate.Tests/bin/Debug/net11.0/Interlocutor.Persistence.NHibernate.Tests`.
  This is a pre-existing, repo-wide environment quirk (confirmed the same failure on
  `Interlocutor.CommonKernel.Tests`), not specific to this sweep.

## What changed

Files touched (all in `tests/Interlocutor.Persistence.NHibernate.Tests/`):

- `NHibernateRepositoryTests.cs` — retrofitted `// Arrange` / `// Act` / `// Assert` section comments onto
  all 4 existing tests (`InsertAsync_...`, `UpdateAsync_...`, `DeleteAsync_...`, `IdSelector_...`). No
  behavioral changes; no coverage gaps auto-filled (no missing member coverage that unit tests can reach —
  see below).
- `NHibernateUnitOfWorkTests.cs` — retrofitted AAA comments onto both existing tests. Fixed a quality
  violation in `Constructor_DoesNotResolveSession_UntilSaveChangesAsyncIsCalled`: the test previously relied
  on "no exception means pass" with zero explicit assertions (violates the "every test has at least one
  assertion" rule). Changed it to wrap construction in `Record.Exception(...)` and assert
  `Assert.Null(exception)`, keeping the same intent (lazy factory never invoked) but making the pass
  condition explicit and using the framework's assertion helper instead of an implicit non-throw.
- `SharedSessionIntegrationTests.cs` — retrofitted `// Arrange` / `// Act` / `// Assert` section comments onto
  the single existing test.

No production code in `src/Interlocutor.Persistence.NHibernate` was changed.

## Coverage gaps identified but not filled

- `NHibernateRepository<TEntity, TKey>.Query`, `GetManyAsync`, and `GetPagedAsync` have no direct unit-test
  coverage in this project. This is a pre-existing, deliberate scope decision already documented in
  `NHibernateRepositoryTests`'s class-level `<remarks>`: `Query` is backed by NHibernate's own
  `ISession.Query<TEntity>()` LINQ provider extension, which resolves real session internals that an
  NSubstitute fake `ISession` cannot stand in for (confirmed by inspection — the substitute has no working
  ADO.NET/session-factory internals for the LINQ provider to walk). `GetManyAsync`/`GetPagedAsync` both
  depend on `Query`, so the same limitation applies transitively. The composition logic itself
  (`DomainRepositoryQueryableExtensions.ApplyFilter`/`ApplySort`/`ApplyPaging`) is covered generically against
  a plain in-memory `IQueryable<T>` by `Interlocutor.Persistence.Abstractions.Tests`, and proven to push down
  to real SQL for one ORM by `Interlocutor.Persistence.EntityFrameworkCore.Tests` (which uses a real
  `Microsoft.Data.Sqlite` in-memory `DbContext`).
  - Closing this gap for NHibernate specifically would require real infrastructure: an actual
    `ISessionFactory`/`ISession` backed by a real (or in-memory) database — NHibernate's SQLite support goes
    through `System.Data.SQLite.Core` plus an XML/Fluent mapping and a `Configuration`/dialect setup, not the
    lightweight `Microsoft.Data.Sqlite` + `DbContext` pattern EF Core uses. That's materially heavier test
    infrastructure and a new package reference, so per the task's guidance this is noted here rather than
    built as part of the sweep.

No `// SWEEP-AMBIGUITY:` or `#warning` annotations were needed — no ambiguous code paths were found that
required a judgment call beyond what's covered above.

## Verification

- `dotnet build tests/Interlocutor.Persistence.NHibernate.Tests/Interlocutor.Persistence.NHibernate.Tests.csproj` — succeeded, 0 warnings, 0 errors.
- `dotnet build src/Interlocutor.Persistence.NHibernate/Interlocutor.Persistence.NHibernate.csproj` — succeeded, 0 warnings, 0 errors.
- Full test run (via the built test host, `net11.0`): 7 total, 7 succeeded, 0 failed, 0 skipped.

- [x] Sweep applied to tests/Interlocutor.Persistence.NHibernate.Tests
- [x] Build passes
- [x] Full test run passes

## Follow-ups

- Consider adding a real-infrastructure test class for `NHibernateRepository<TEntity, TKey>.Query` /
  `GetManyAsync` / `GetPagedAsync` (e.g. a SQLite-backed `ISessionFactory`, mirroring what
  `Interlocutor.Persistence.EntityFrameworkCore.Tests` does with `Microsoft.Data.Sqlite`), if NHibernate-
  specific SQL pushdown behavior for these three members needs direct proof rather than relying on the
  shared `DomainRepositoryQueryableExtensions` coverage plus the EF Core pushdown proof. Needs a human call on
  whether the added `System.Data.SQLite.Core` dependency and mapping setup are worth it for this package.
