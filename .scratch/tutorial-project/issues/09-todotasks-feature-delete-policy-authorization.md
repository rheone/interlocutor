Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 9's core (not the "no payload, caller-chosen outcome type" sub-section - that's ticket 10), at `Features/Tasks/DeleteTodoTask/` (**not** `Features/TodoTasks/` - see ticket 05's Comments for why):

- **`DeleteTodoTaskCommand`**/handler: `ICommand<OperationOutcome<Success, NotFound, Forbidden>>`, `IRequiresPolicy` (`PolicyName => "DeleteTodoTask"`), `IAuditableRequest` - as written in the guide.
- **`Infrastructure/TodoTaskPolicyEvaluator.cs`**: `IPolicyEvaluator`, as written in the guide (checks the caller is authenticated for the `"DeleteTodoTask"` policy; a short comment noting this is a stand-in for a real policy engine, exactly as the guide's own comment says).
- Endpoint: `DELETE /todo-tasks/{id:int}` - as written in the guide.
- A code comment here should point at ticket 07's `UpdateTodoTaskHandler` as the contrasting fine-grained/`ResourceCommandHandler` authorization style, mirroring the guide's own cross-reference between its Steps 7 and 9.

## Acceptance criteria

- [x] `DeleteTodoTaskCommand`/handler/`TodoTaskPolicyEvaluator` exist, XML-documented, cross-reference ticket 07's contrasting style
- [x] Unit tests: successful delete, not-found; a policy-evaluator unit test proving it denies when unauthenticated, allows when authenticated for `"DeleteTodoTask"`, and denies an unrelated policy name
- [x] HTTP integration test: an authenticated caller can delete an existing task (task is gone afterward, verified via a subsequent `GET` returning not-found); an unauthenticated request is rejected before the handler ever loads the entity (asserted by confirming the task still exists afterward)
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier .` all clean (896/896 solution-wide)

## Blocked by

05

## Comments

Minor style note, not a functional finding: `new Success()` (exactly as the guide's own example writes it) trips this repo's `SA1129` ("do not use default value type constructor") since `Success` is a `readonly record struct` - used `default(Success)` instead, matching the pattern this repo's own library tests already use (e.g. `NHibernateRepositoryTests`).
