# 02 - Project & test-harness scaffolding (MS DI + EF Core)

**What to build:** A fresh `TutorialProject/TodoTasks.Api` minimal API project on `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.Persistence.EntityFrameworkCore`, with `interlocutor.Pipeline.UseRecommendedChain()` wired the same way the original guide describes for the MS DI path. Persistence: EF Core over `Microsoft.EntityFrameworkCore.Sqlite`, in-memory (`Data Source=:memory:` on one kept-open connection, shared across the app/test-fixture instance's scoped sessions). Vertical-slice-by-directory structure under `Features/`, mirroring the original project's shape. Test project(s) scaffolded the same way (unit/persistence/HTTP layers), both added to `Interlocutor.slnx`.

**Blocked by:** 01

**Status:** done

- [x] `dotnet build` and `dotnet test` succeed from the repo root with an empty `Program.cs` host and an empty `DbContext`
- [x] The in-memory SQLite connection strategy is proven with a smoke test: data written in one scope is readable in another scope within the same app/test-fixture instance
- [x] Project structure matches the vertical-slice-by-directory convention (one folder per use case under `Features/`) - no `Features/` yet since there are no features until ticket 03+; `Persistence/`/`Infrastructure/` scaffolding is in place for them to land into

## Comments

**2026-09-04** - Implemented: fresh `TodoTasks.Api.csproj` (minimal API, MS DI + EF Core Sqlite) and an empty `TodoTasksDbContext`. `Program.cs` wires a singleton kept-open `SqliteConnection` (`Data Source=:memory:`) shared across scoped `DbContext` instances, `AddInterlocutorUnitOfWork`/repository wiring, and the pipeline spelled out as `UseRecommendedChain()`'s five individual steps rather than the sugar method itself - `AddUnitOfWorkBehavior()` throws under MS DI once EF Core's arity-3 `UnitOfWorkBehavior<TRequest,TResponse,TContext>` is in play (documented limitation, `documentation/transactions-and-store-integration.md`); a `TodoTasksUnitOfWorkBehavior<TRequest,TResponse>` closes `TContext` over `TodoTasksDbContext` and registers via `AddBehavior` instead - the same workaround that doc already prescribes. `tests/TodoTasks.Api.Tests` scaffolded with a `WebApplicationFactory<Program>`-based fixture; `InMemorySqliteConnectionPersistenceTests` proves data written via one DI scope's `DbContext` is readable via another's, against the real host wiring (not a re-implemented test-only copy). Both projects re-added to `Interlocutor.slnx`. Full solution build (0 warnings, 0 errors) and test (1066/1066) green.
