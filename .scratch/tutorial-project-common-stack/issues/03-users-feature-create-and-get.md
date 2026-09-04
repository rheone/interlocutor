# 03 - Users feature: Create & Get

**What to build:** `User` entity (Id, DisplayName, Email) mapped via EF Core. Create and Get endpoints/commands/queries/handlers, following the vertical-slice convention. `CreateUserCommand` gets a duplicate-email `Conflict` check.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] `POST` creates a user; a duplicate email returns the `Conflict` case, not a generic `Failure`
- [ ] `GET` retrieves a user by id, including the not-found case
- [ ] Unit tests (mocked repository) and persistence tests (real in-memory SQLite via EF Core) both cover the create/get paths
