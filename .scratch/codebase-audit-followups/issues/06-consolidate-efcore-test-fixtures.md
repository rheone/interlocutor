# 06 - Consolidate EF Core test fixtures onto IClassFixture

**What to build:** `EntityFrameworkRepositoryTests.cs`, `EntityFrameworkUnitOfWorkTests.cs`, `SharedSessionIntegrationTests.cs`, `UnitOfWorkBehaviorTests.cs`, and `UnitOfWorkSaveChangesInterceptorTests.cs` in `Interlocutor.Persistence.EntityFrameworkCore.Tests` each redeclare a near-identical private `Widget` entity class and `TestDbContext` plus identical Sqlite-connection/`EnsureCreated` setup. Consolidate these into a shared fixture used via `IClassFixture<>`, following the pattern the sibling `Interlocutor.Persistence.NHibernate.Tests` project already uses (a shared top-level `Widget.cs` and `RealSessionFixture.cs`).

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] A single shared `Widget`/`TestDbContext` fixture exists for `Interlocutor.Persistence.EntityFrameworkCore.Tests`, consumed via `IClassFixture<>`
- [ ] The five test files no longer each declare their own copy
- [ ] All existing test cases still pass with equivalent coverage (no test deleted, only its setup consolidated)
