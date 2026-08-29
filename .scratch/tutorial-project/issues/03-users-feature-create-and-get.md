Status: done

## Labels

ready-for-agent

## What to build

The first vertical slice, and the first entity beyond the guide itself. Lives at `Features/Users/CreateUser/` and `Features/Users/GetUserById/`.

- **`Domain/User.cs`**: a class NHibernate maps and tracks - `Id` (int, protected setter), `DisplayName` (string), `Email` (string) - following the same shape/comment conventions as the guide's own `TodoTask` (protected parameterless constructor for NHibernate proxies, virtual members for lazy-loading/change-tracking, an XML doc comment explaining both, matching `documentation/beginners-guide-todo-api.md` Step 3's `TodoTask` almost line for line).
- **`Domain/UserDto.cs`**: the narrower response shape returned to callers, mirroring `TodoTaskDto.FromEntity`.
- **`Persistence/UserMap.cs`**: Fluent NHibernate `ClassMap<User>`, mirroring `TodoTaskMap`'s shape (`Not.Nullable()`, sensible `Length(...)` bounds on `DisplayName`/`Email`).
- Repository registration: `AddInterlocutorRepository<User, int, NHibernateRepository<User, int>>(...)`, alongside the existing `TodoTask` registration point from ticket 02 (there's no `TodoTask` repository yet at this point in the sequence - just add the registration call in the same place ticket 02 left a comment for it, or start that block now if ticket 02 didn't).
- **`Features/Users/CreateUser/CreateUserCommand.cs`** + validator + handler + endpoint: `ICommand<OperationOutcome<UserDto, ValidationErrors, Conflict>>`. Validator (FluentValidation) requires non-empty `DisplayName` and a well-formed `Email`. Handler checks for an existing `User` with the same `Email` first (same pattern as `CreateTodoTaskHandler`'s duplicate-title check) and returns `Conflict` on a match; otherwise inserts and returns the `UserDto`. `POST /users`.
- **`Features/Users/GetUserById/GetUserByIdQuery.cs`** + handler + endpoint: `IQuery<OperationOutcome<UserDto, NotFound>>`, mirroring `GetTodoTaskByIdQuery`/`GetTodoTaskByIdHandler` exactly. `GET /users/{id:int}`.
- Neither endpoint requires authentication - User management itself isn't gated behind a caller identity in this POC (only `TodoTask` operations are, starting ticket 04/05).

Every command/query/handler/DTO/validator/map gets a rich XML doc comment - for a junior developer reading this cold, not just to satisfy the analyzer. Where a comment would just restate the guide, point at it instead (e.g. "mirrors `TodoTaskMap` - see `documentation/beginners-guide-todo-api.md` Step 4").

## Acceptance criteria

- [x] `User`/`UserDto`/`UserMap` exist, XML-documented, following the guide's `TodoTask`/`TodoTaskDto`/`TodoTaskMap` conventions
- [x] `CreateUserCommand` unit tests (NSubstitute-mocked repository/unit-of-work): success path, validation failure, duplicate-email `Conflict`
- [x] `GetUserByIdQuery` unit tests: found and not-found paths
- [x] Persistence/mapping integration test proves a `User` round-trips through the real in-memory SQLite `UserMap`
- [x] HTTP integration test: `POST /users` then `GET /users/{id}` returns the created user; `POST /users` with a duplicate email returns a conflict response; `GET /users/{id}` for a missing id returns a not-found response
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (838/838 solution-wide)

## Blocked by

02

## Comments

The HTTP-layer tests (`Http/UsersEndpointTests.cs`) surfaced two real bugs that the unit tests alone couldn't catch, since both only manifest across two independent request-scoped sessions - exactly the gap "unit tests mock the repository" leaves open, which is why this ticket's persistence/HTTP layers exist at all. Both are documented in code, not just here, since they're genuinely non-obvious and will bite the next feature (ticket 05+) if forgotten:

1. **In-memory SQLite needs shared-cache mode, not a single shared connection object.** The originally-planned approach (ticket 02, PRD.md: one `SQLiteConnection` object, force every `ISession` to open against it via `WithOptions().Connection(...)`) compiles, passes a simple two-session smoke test, and then silently loses data between real, independently-scoped sessions - NHibernate's own session lifecycle resets the shared in-memory database somewhere in there (verified empirically: neither `ConnectionReleaseMode.OnClose` nor `AfterTransaction` changed the outcome; a raw ADO.NET query through the very same connection object confirmed the data was actually gone, not just invisible to NHibernate). Replaced with SQLite's own shared-cache URI (`FullUri=file:{unique-name}?mode=memory&cache=shared`), one anchor connection kept alive purely to hold the cache open, every session opening its own independent connection the normal NHibernate way. The name has to be unique per `Build()` call - a bare `file::memory:?cache=shared` is process-wide and would silently let every caller (the app, every test fixture) share one database. See `Persistence/TodoTasksSessionFactory.cs`'s own remarks for the full account, and `.scratch/tutorial-project/PRD.md`'s persistence-decision note (updated to match).
2. **`UnitOfWorkBehavior`'s commit/rollback decision needs its own `IOutcomeClassifier` registration, separate from `IResponseOutcomeDescriber`.** `OutcomeClassifierBuilder.DefaultClassifier` - the fallback used everywhere nothing else is registered - only recognizes the library's own kernel case types (`Success`, `NotFound`, `Conflict`, ...); any project-specific success DTO used as an `OperationOutcome<TSuccess,...>`'s `TSuccess` (i.e. `UserDto`, and every future one) is fail-closed: treated as a failure, so `UnitOfWorkBehavior` silently rolled back every genuinely successful write while the handler still returned what looked like a normal success outcome to its caller. `interlocutor.Pipeline.OverrideOutcomeDescriber<TDescriber>()` alone does **not** fix this - it only replaces the *logging/description* half (`IResponseOutcomeDescriber`); the commit/rollback decision reads a separate `IOutcomeClassifier` constructor parameter that needs its own explicit DI registration. Fixed with `Infrastructure/TodoTasksOutcomeDescriber.cs` (registers `UserDto` as a success type) plus **both** registrations in `Program.cs` - `OverrideOutcomeDescriber<TodoTasksOutcomeDescriber>()` and `containerBuilder.RegisterInstance(TodoTasksOutcomeDescriber.Classifier).As<IOutcomeClassifier>()`. Every future feature's new success DTO (`TodoTaskDto`, ticket 05+) needs a line added to `TodoTasksOutcomeDescriber.Classifier` or it will hit the exact same silent-rollback bug.

Neither bug is specific to the Users feature - both apply to every write handler this project will ever add. Diagnosed by direct reproduction (a scratchpad NHibernate+SQLite console prototype for bug 1; temporary `Console.WriteLine` tracing inside `UnitOfWorkBehavior.Handle` - reverted afterward, no library files changed - for bug 2), not by guessing from documentation alone.
