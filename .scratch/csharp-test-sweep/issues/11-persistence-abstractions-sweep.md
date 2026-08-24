# 11 - Interlocutor.Persistence.Abstractions test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.Persistence.Abstractions.Tests
- [x] Build passes
- [x] Full test run passes (verified via the native Microsoft.Testing.Platform executable — see Follow-ups for a pre-existing, repo-wide `dotnet test` CLI environment issue unrelated to this sweep)

## What was done

**Scope:** full-project sweep of `src/Interlocutor.Persistence.Abstractions` (xUnit v3 + NSubstitute test project) against `tests/Interlocutor.Persistence.Abstractions.Tests`.

### Discovery phase (before touching tests)

- Test framework: xUnit v3. Mocking library: NSubstitute (referenced in the `.csproj` but unused by any test in this project — no collaborators to mock, since the production code is pure data types + LINQ composition + one default interface method).
- Duplicate test IDs: none (`--list-tests` against the built native runner showed 16 unique display names, no duplicates).
- Skipped tests: none (`[Fact(Skip` / `[Theory(Skip` grep returned nothing).
- Assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.): none found.
- `async void` test methods: none found (project has no async production surface to test — see Follow-ups).
- `TheoryData<T>` / custom-serializer usage: none — existing theories use `[InlineData]` with primitive `int`/`bool` types only, no serializer registration needed.
- `InternalsVisibleTo`: not applicable — the production project has zero `internal` members; everything under test is public.
- Project hygiene: test `.csproj` has no `IsPackable` override (inherits repo default), references modern `xunit.v3` + `Microsoft.Testing.Platform` + `NSubstitute` — no outdated framework packages.
- Missing test files: `IUnitOfWork.cs`, `SortCriterion.cs`, `SortDirection.cs` have no dedicated test file. Evaluated and intentionally left uncovered — see Follow-ups/rationale below; not created as skeletons since none would exercise any logic.
- Duplicate-test-pattern candidates for parameterization: none beyond the two `[Theory]`s already in place (`ApplyPaging_Throws_WhenPageNumberOrPageSizeIsLessThanOne`, `PageTests.TotalPageCount_IsCeilingOfTotalCountOverPageSize`), both already parameterized appropriately.
- `[Obsolete]`-marked members: none exist in this package, so the "wrap in `#pragma warning disable/restore CS0618`" instruction had nothing to apply to.
- Real infrastructure (DB/network/filesystem): **not required**. This package is a pure abstraction (interfaces, records, LINQ-over-`IQueryable` extension methods) with no store-specific implementation in scope — all existing and added tests run against in-memory fakes/`IQueryable` sources.

### Files touched

- `tests/Interlocutor.Persistence.Abstractions.Tests/DomainRepositoryQueryableExtensionsTests.cs`
  - Retrofitted `// Act` / `// Assert` (and `// Act & Assert` for one-liner cases) section comments onto all pre-existing test methods.
  - **Coverage gap filled:** added `ApplySort_OrdersByPrimaryKey_Descending` (tagged `// Auto Generated, verify expected behavior:`) — the existing tests exercised `Ascending` only on the primary sort key and `Descending` only on a tie-breaking secondary key, leaving the `Descending`-on-primary-key branch of `ApplySort`'s conditional untested.
- `tests/Interlocutor.Persistence.Abstractions.Tests/DomainRepositoryGetByIdTests.cs`
  - Retrofitted `// Arrange` / `// Act` / `// Assert` section comments onto both pre-existing test methods.
  - **Coverage gap filled:** added `GetById_ReturnsNull_WhenRepositoryIsEmpty` (tagged `// Auto Generated, verify expected behavior:`) — a boundary case distinct from "key absent among several entities": an empty `Query` source hitting `FirstOrDefault` with zero candidates.
- `tests/Interlocutor.Persistence.Abstractions.Tests/PageTests.cs`
  - Retrofitted `// Arrange` / `// Act & Assert` section comments onto the existing theory method.

No production code (`src/Interlocutor.Persistence.Abstractions`) was changed.

### Quality fixes applied

- AAA section comments were the only quality violation found in the existing suite (no anti-pattern assertions, no `async void`, no unmocked-collaborator mistakes, no magic-value duplication needing extraction, no ambiguous assertion failures). All three existing test files now carry `// Arrange` / `// Act` / `// Assert` (or the combined `// Act & Assert` form where one statement covers both) on every test method, new and pre-existing.

### `// SWEEP-AMBIGUITY:` / `#warning` annotations

None left — no ambiguous production behavior was encountered; both coverage gaps had an unambiguous correct answer derivable from the existing implementation and were filled directly.

### Flagged but not fixed

- `SortCriterion<TEntity>` and `SortDirection` have no dedicated test file. Both are plain data carriers with no computed members or branching logic of their own (`SortCriterion` is a two-property record, `SortDirection` a two-value enum); their only observable behavior is exercised transitively through `DomainRepositoryQueryableExtensionsTests`. Per the "simple record/struct" convention in `CLAUDE.md`/AGENTS.md, no dedicated test file was created — flagging here rather than silently skipping, in case reviewers want explicit record-equality/deconstruction tests anyway.
- `IUnitOfWork.SaveChangesAsync` has no test file. It's a bare interface member with no default implementation and no logic in this package — there's nothing for this package's test suite to exercise (implementations belong to `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate`, out of this ticket's scope). Not treated as a coverage gap.

## Follow-ups

- Environment note (not caused by this sweep, repo-wide): `dotnet test` fails in this sandbox with `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later` on every project checked, including an untouched one (`Interlocutor.CommonKernel.Tests`) — confirmed pre-existing, unrelated to these changes. Final verification instead used `dotnet build` (0 warnings/errors) followed by running the built native Microsoft.Testing.Platform executable directly (`tests/Interlocutor.Persistence.Abstractions.Tests/bin/Debug/net11.0/Interlocutor.Persistence.Abstractions.Tests`), which reported `18/18 passed, 0 failed, 0 skipped`. Someone should fix the `dotnet test` CLI/MTP opt-in mismatch in this environment separately — it blocks the documented `dotnet test` workflow in `CLAUDE.md` for every project, not just this one.
- The two "missing test file" items in Flagged-but-not-fixed are a judgment call (simple data-carrier types with no logic of their own); revisit only if a reviewer wants explicit record-equality tests for `SortCriterion<TEntity>`.
