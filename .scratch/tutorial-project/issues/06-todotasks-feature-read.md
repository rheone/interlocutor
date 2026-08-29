Status: open

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 6's core (not the QueryOver/paging Bonus sub-sections - those are backlog tickets 14/15), at `Features/TodoTasks/GetTodoTaskById/` and `Features/TodoTasks/ListTodoTasks/`:

- **`GetTodoTaskByIdQuery`**/handler: `IQuery<OperationOutcome<TodoTaskDto, NotFound>>`, as written in the guide. `GET /todo-tasks/{id:int}`.
- **`ListTodoTasksQuery`**/handler: `IQuery<OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>>`, `OnlyIncomplete` filter, sorted newest-first, as written in the guide. `GET /todo-tasks?onlyIncomplete=...`.
- Neither has a marker interface, neither writes - no transaction opens for either, which the tests below should demonstrate rather than assert blindly.
- Neither endpoint requires authentication in the guide's own running example - keep it that way here too, unless doing so makes the HTTP integration tests meaningfully harder to write against seeded data (in which case use the existing dev-auth header purely to seed data via the write endpoints, not to gate the read ones).

## Acceptance criteria

- [ ] `GetTodoTaskByIdQuery`/`ListTodoTasksQuery` and their handlers exist, XML-documented
- [ ] Unit tests: get-by-id found/not-found; list with/without the `OnlyIncomplete` filter, confirming sort order
- [ ] HTTP integration test: create a couple of tasks (via ticket 05's endpoint), then `GET` each by id and `GET` the list both filtered and unfiltered, asserting the right tasks come back in the right order
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

05
