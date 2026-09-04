# 05 - TodoTasks feature: Create

**What to build:** `TodoTask` entity mapped via EF Core (`OwnerId` stays an opaque string, no database foreign key to `User.Id`, matching the guide's own modeling). Create endpoint/command/handler with a duplicate-title `Conflict` check, mirroring the Users feature's pattern.

**Blocked by:** 04

**Status:** ready-for-agent

- [ ] `POST` creates a task owned by the authenticated caller; a duplicate title returns `Conflict`
- [ ] Unit and persistence tests cover the create path, including the conflict case
