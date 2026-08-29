Status: open

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 9's core (not the "no payload, caller-chosen outcome type" sub-section - that's ticket 10), at `Features/Tasks/DeleteTodoTask/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`DeleteTodoTaskCommand`**/handler: `ICommand<OperationOutcome<Success, NotFound, Forbidden>>`, `IRequiresPolicy` (`PolicyName => "DeleteTodoTask"`), `IAuditableRequest` - as written in the guide.
- **`Infrastructure/TodoTaskPolicyEvaluator.cs`**: `IPolicyEvaluator`, as written in the guide (checks the caller is authenticated for the `"DeleteTodoTask"` policy; a short comment noting this is a stand-in for a real policy engine, exactly as the guide's own comment says).
- Endpoint: `DELETE /todo-tasks/{id:int}` - as written in the guide.
- A code comment here should point at ticket 07's `UpdateTodoTaskHandler` as the contrasting fine-grained/`ResourceCommandHandler` authorization style, mirroring the guide's own cross-reference between its Steps 7 and 9.

## Acceptance criteria

- [ ] `DeleteTodoTaskCommand`/handler/`TodoTaskPolicyEvaluator` exist, XML-documented, cross-reference ticket 07's contrasting style
- [ ] Unit tests: successful delete, not-found; a policy-evaluator unit test proving it denies when unauthenticated and allows when authenticated for `"DeleteTodoTask"`
- [ ] HTTP integration test: an authenticated caller can delete an existing task (task is gone afterward, verified via a subsequent `GET` returning not-found); an unauthenticated request is rejected before the handler ever loads the entity (assert the entity still exists afterward, proving the policy check actually ran first)
- [ ] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean

## Blocked by

05
