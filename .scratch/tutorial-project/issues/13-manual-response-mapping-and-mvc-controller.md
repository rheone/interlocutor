Status: backlog - deferred, do not start until explicitly promoted to ready-for-agent

## Labels

needs-triage

## What to build

`documentation/beginners-guide-todo-api.md` Step 5's "Bonus: managing the response manually" section - a manual `switch` on an outcome's `.Value` instead of `ToMinimalApiResult()` for one endpoint (e.g. a `/todo-tasks/{id:int}/manual` variant of the GetById query), plus the MVC-controller equivalent (`ToActionResult()`, a `[ApiController]` class) as an alternative transport for the same query. Advanced-feature backlog item - demonstrates that `ToMinimalApiResult()`/`ToActionResult()` are convenience wrappers, not the only option, and that minimal APIs and MVC controllers are interchangeable at the outcome-mapping boundary.

## Acceptance criteria

- [ ] A manual-mapping minimal API endpoint and an equivalent MVC controller action both exist, each with a passing HTTP integration test
- [ ] A code comment explains the trade-off the guide itself calls out: centralized/consistent HTTP mapping vs. per-endpoint control

## Blocked by

12
