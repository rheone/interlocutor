# 02 - Project & test-harness scaffolding (MS DI + EF Core)

**What to build:** A fresh `TutorialProject/TodoTasks.Api` minimal API project on `Interlocutor.DependencyInjection.Microsoft` and `Interlocutor.Persistence.EntityFrameworkCore`, with `interlocutor.Pipeline.UseRecommendedChain()` wired the same way the original guide describes for the MS DI path. Persistence: EF Core over `Microsoft.EntityFrameworkCore.Sqlite`, in-memory (`Data Source=:memory:` on one kept-open connection, shared across the app/test-fixture instance's scoped sessions). Vertical-slice-by-directory structure under `Features/`, mirroring the original project's shape. Test project(s) scaffolded the same way (unit/persistence/HTTP layers), both added to `Interlocutor.slnx`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `dotnet build` and `dotnet test` succeed from the repo root with an empty `Program.cs` host and an empty `DbContext`
- [ ] The in-memory SQLite connection strategy is proven with a smoke test: data written in one scope is readable in another scope within the same app/test-fixture instance
- [ ] Project structure matches the vertical-slice-by-directory convention (one folder per use case under `Features/`)
