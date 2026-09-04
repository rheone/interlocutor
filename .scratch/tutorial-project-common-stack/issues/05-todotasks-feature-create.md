# 05 - TodoTasks feature: Create

**What to build:** `TodoTask` entity mapped via EF Core (`OwnerId` stays an opaque string, no database foreign key to `User.Id`, matching the guide's own modeling). Create endpoint/command/handler with a duplicate-title `Conflict` check, mirroring the Users feature's pattern.

**Blocked by:** 04

**Status:** done

- [x] `POST` creates a task owned by the authenticated caller; a duplicate title returns `Conflict`
- [x] Unit and persistence tests cover the create path, including the conflict case

## Comments

**2026-09-04** - Implemented: `TodoTask` entity (Id, Title, Notes, OwnerId: string, IsCompleted, CreatedAtUtc) mapped via EF Core with `OwnerId` as a plain string column, no FK to `User` - matching the guide's modeling. `CreateTodoTaskRequest` has no `OwnerId` member at all, so a caller cannot supply one; the endpoint (`.RequireAuthorization()`) sources it from `IRequestContextAccessor` via a new `RequireUserId()` extension (ported from the shelved project - every later authenticated endpoint needs the same helper). Duplicate-title check is scoped per-owner (`OwnerId` + `Title`), matching the shelved project and the guide's Step 5, with a matching unique index as schema-level backstop; a persistence test proves two different owners can share a title. Unit, persistence, and HTTP tests all cover the create/conflict paths. Full solution build (0 errors) and test (1094/1094) green.
