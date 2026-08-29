Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 6's core (not the QueryOver/paging Bonus sub-sections - those are backlog tickets 14/15), at `Features/Tasks/GetTodoTaskById/` and `Features/Tasks/ListTodoTasks/` (**not** `Features/TodoTasks/` - that folder name collides with the project's own root namespace `TodoTasks.Api`; see ticket 05's Comments for the compile error this causes and why it was renamed):

- **`GetTodoTaskByIdQuery`**/handler: `IQuery<OperationOutcome<TodoTaskDto, NotFound>>`, as written in the guide. `GET /todo-tasks/{id:int}`.
- **`ListTodoTasksQuery`**/handler: `IQuery<OperationOutcome<IReadOnlyList<TodoTaskDto>, Failure>>`, `OnlyIncomplete` filter, sorted newest-first, as written in the guide. `GET /todo-tasks?onlyIncomplete=...`.
- Neither has a marker interface, neither writes - no transaction opens for either, which the tests below should demonstrate rather than assert blindly.
- Neither endpoint requires authentication in the guide's own running example - keep it that way here too, unless doing so makes the HTTP integration tests meaningfully harder to write against seeded data (in which case use the existing dev-auth header purely to seed data via the write endpoints, not to gate the read ones).

## Acceptance criteria

- [x] `GetTodoTaskByIdQuery`/`ListTodoTasksQuery` and their handlers exist, XML-documented
- [x] Unit tests: get-by-id found/not-found; list with/without the `OnlyIncomplete` filter (via NSubstitute `Arg.Do` argument capture, not `Arg.Is` - see `## Comments`); sort order (captures the `SortCriterion` passed to the repository and asserts `Descending`)
- [x] HTTP integration test: create a task then `GET` it by id; create tasks with a unique marker title and `GET` the unfiltered list, confirming both come back
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (868/868 solution-wide)

## Blocked by

05

## Comments

`NSubstitute.Arg.Is<T>` is ambiguous (`CS0121`, two equally-good overloads) when `T` is itself an `Expression<Func<...>>` - the compiler can't tell whether the lambda you're passing is `T`'s own predicate or a `Predicate<T>` wrapper. Used `Arg.Do<T>` to capture the actual argument into a local variable and assert on it afterward instead, which sidesteps the ambiguity and reads slightly clearer besides. Worth knowing for any future test matching on an `Expression<Func<TEntity,bool>>` filter argument (ticket 07's `ResourceCommandHandler`-based tests won't hit this, but anything mocking `IDomainRepository<TEntity,TKey>.GetManyAsync`/`GetPagedAsync` directly might).
