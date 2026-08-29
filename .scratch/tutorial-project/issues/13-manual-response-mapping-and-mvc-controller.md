Status: done - promoted and completed with expanded scope (see Comments)

## Labels

needs-triage

## What to build

`documentation/beginners-guide-todo-api.md` Step 5's "Bonus: managing the response manually" section - a manual `switch` on an outcome's `.Value` instead of `ToMinimalApiResult()` for one endpoint (e.g. a `/todo-tasks/{id:int}/manual` variant of the GetById query), plus the MVC-controller equivalent (`ToActionResult()`, a `[ApiController]` class) as an alternative transport for the same query. Advanced-feature backlog item - demonstrates that `ToMinimalApiResult()`/`ToActionResult()` are convenience wrappers, not the only option, and that minimal APIs and MVC controllers are interchangeable at the outcome-mapping boundary.

## Acceptance criteria

- [x] An MVC controller action exists as an alternative transport for the same query, with a passing HTTP integration test
- [x] A code comment explains the trade-off the guide itself calls out: centralized/consistent HTTP mapping vs. per-endpoint control

## Blocked by

12

## Comments

Promoted directly (explicit user request), with materially expanded scope: instead of one controller action for one query, the user asked for full controller parity per domain, plus proper IoC, plus a visible pipeline (logging) demonstration. Delivered:

- `TodoTasksController` and `UsersController` (`Features/Tasks/TodoTasksController.cs`, `Features/Users/UsersController.cs`) - one controller per domain, every existing minimal-API endpoint gets a controller-action twin, under `/api/todo-tasks` and `/api/users` (a different route prefix than the root-level minimal API, required to avoid `AmbiguousMatchException` - the two transports can't share a route template).
- `TodoTasksController.GetById` does the manual `switch` on the outcome's `.Value` this ticket originally scoped as a *minimal API* variant (`/todo-tasks/{id:int}/manual`) - but as a controller action instead, since the controller itself already demonstrates the identical underlying concept (bypassing `ToActionResult()`/`ToMinimalApiResult()` to pattern-match the union by hand). The minimal-API `/manual` variant was intentionally **not** built - it would have been a second demonstration of the same idea, not a new one, since the controller's `GetById` and the rest of this domain's minimal-API endpoints already show both mapping styles side by side.
- `UsersController.GetById` calls `ToActionResult()` for direct contrast, so both styles are visible on the identically-shaped `OperationOutcome<TDto, NotFound>` union.
- Both controllers use primary-constructor injection of `ISender`/`IRequestContextAccessor` - no pipeline or DI registration changes were needed; `Program.cs` only gained `AddControllers().AddControllersAsServices()` and `MapControllers()`. Dispatching from a controller runs through the exact same registered pipeline (Logging → Auditing → Authorization → Validation → UnitOfWork) as the minimal-API endpoints, confirmed by matching `LoggingBehavior` console output for both transports.
- New HTTP integration tests: `tests/TodoTasks.Api.Tests/Http/TodoTasksControllerTests.cs`, `UsersControllerTests.cs`.
- `documentation/sample-app-walkthrough.md` updated in the same iteration (project structure, feature table, "Transport conversion at the edge", running/testing `curl` examples).
