Status: done

## Labels

ready-for-agent

## What to build

The actual point of the User feature: a real, working example of the guide's "multiple repositories, one unit of work" pattern (`documentation/beginners-guide-todo-api.md` Step 8's Bonus section describes the pattern via an illustrative activity-log entity; this ticket is the same pattern with real, load-bearing domain behavior). Lives at `Features/Users/DeleteUser/`.

- **`DeleteUserCommand`**: `ICommand<OperationOutcome<Success, NotFound>>` (or add `IAuditableRequest` if it fits the project's conventions established by this point - use your judgement, matching how the rest of the project uses that marker).
- **`DeleteUserHandler`**: injects `IDomainRepository<User, int>`, `IDomainRepository<TodoTask, int>`, and `IUnitOfWork`.
  1. Load the user (`NotFound` if missing).
  2. `GetManyAsync(t => t.OwnerId == user.Id.ToString(), ...)` (or whatever the actual `OwnerId`/`User.Id` type comparison needs to be - `OwnerId` is a string per the guide's own modeling, `User.Id` is an int; convert consistently with however ticket 04's auth handler already does this comparison) against the `TodoTask` repository, and delete every one of them.
  3. Delete the user.
  4. One `unitOfWork.SaveChangesAsync()` call commits the task deletions and the user deletion together.
- A prominent XML doc comment (on the handler class, not just a code comment) should state explicitly: this is the project's worked example of a single `IUnitOfWork` spanning two repositories, and should point at `documentation/beginners-guide-todo-api.md`'s "Bonus: multiple repositories, one unit of work" section for the fuller explanation of why that matters (transactional atomicity - the user and their tasks are deleted together or not at all).
- Endpoint: `DELETE /users/{id:int}`, unauthenticated (matching ticket 03's decision that User management itself isn't gated behind caller identity in this POC).

## Acceptance criteria

- [x] `DeleteUserCommand`/`DeleteUserHandler` exist, XML-documented, explicitly call out the multi-repository/one-unit-of-work pattern and point at the guide's Bonus section
- [x] Unit test (NSubstitute-mocked repositories/unit-of-work): deleting a user with tasks deletes both the user and every one of their tasks, via exactly one `SaveChangesAsync` call; deleting a user with no tasks still succeeds; deleting a missing user returns not-found and touches neither repository
- [x] Integration test against the real in-memory SQLite persistence (not just mocks): create a user, create a couple of tasks for them, delete the user, then confirm both the user row and every one of their task rows are actually gone from the database - and that a *different* user's tasks are untouched
- [x] HTTP integration test covering the same end-to-end flow through the real endpoints
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

03, 05

## Comments

- `DeleteUserCommand` does **not** carry `IAuditableRequest`. The rest of the Users feature (`CreateUserCommand`) doesn't audit either - only `ITransactionalRequest` where a race actually matters - so this stays consistent with that feature's own convention rather than borrowing the Tasks feature's (`CreateTodoTaskCommand`/`DeleteTodoTaskCommand`/etc.) auditing choice. `DELETE /users/{id}` is also unauthenticated (ticket 03's decision carried forward), so there's no caller identity here worth auditing against anyway.
- `TodoTask.OwnerId` is an opaque string, not a foreign key to `User.Id` (same modeling `DevHeaderAuthenticationHandler` already relies on) - the cascade query is `t.OwnerId == user.Id.ToString(CultureInfo.InvariantCulture)`, a string match, not a relational join.
- The persistence integration test (`Persistence/DeleteUserCascadeTests.cs`) surfaced a real gotcha worth recording for future tickets: `NHibernateUnitOfWork.SaveChangesAsync` only *begins a transaction and flushes* - it deliberately leaves the actual commit to `UnitOfWorkBehavior` at the end of a real pipeline dispatch (see that type's own remarks). A test that calls a handler directly, bypassing the pipeline - as this one does, to exercise the handler against a real database without standing up the whole app - has to commit the session's transaction itself, or the transaction is implicitly rolled back when the session is disposed and the test's own assertions fail for a misleading reason. `Persistence/UserMapTests.cs`/`TodoTaskMapTests.cs` avoid this by never going through `IUnitOfWork` at all (they flush directly on the session, outside any transaction, relying on SQLite's autocommit) - this ticket's seeding step follows that same pattern, and only the actual handler-under-test step needs the explicit commit.
- Bare `Success` maps to `200 OK` through `ToMinimalApiResult()` (confirmed here, and matches `DeleteTodoTaskEndpointTests`), not `204 No Content` - worth remembering before assuming REST convention over the library's actual mapping.
