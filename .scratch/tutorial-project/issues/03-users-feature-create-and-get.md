Status: open

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

- [ ] `User`/`UserDto`/`UserMap` exist, XML-documented, following the guide's `TodoTask`/`TodoTaskDto`/`TodoTaskMap` conventions
- [ ] `CreateUserCommand` unit tests (NSubstitute-mocked repository/unit-of-work): success path, validation failure, duplicate-email `Conflict`
- [ ] `GetUserByIdQuery` unit tests: found and not-found paths
- [ ] Persistence/mapping integration test proves a `User` round-trips through the real in-memory SQLite `UserMap`
- [ ] HTTP integration test: `POST /users` then `GET /users/{id}` returns the created user; `POST /users` with a duplicate email returns a conflict response; `GET /users/{id}` for a missing id returns a not-found response
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

02
