# 09 - TodoTasks feature: Delete + policy authorization

**What to build:** Delete endpoint/command/handler for TodoTasks, gated by `IRequiresPolicy`-based authorization (matching the original project's use of `AuthorizationBehavior`).

**Blocked by:** 05

**Status:** done

- [x] Deleting an owned task succeeds; deleting a non-owned task is rejected by the authorization policy before the handler runs
- [x] Unit and persistence tests cover both paths

## Comments

**2026-09-04** - Implemented: `TodoTasksPolicyEvaluator` (`IPolicyEvaluator`) checks task ownership via `IDomainRepository<TodoTask, int>` against the caller's id from `IRequestContextAccessor`, registered ahead of `AddAuthorizationBehavior()`'s try-added default so it wins. `DeleteTodoTaskCommand` implements `IRequiresPolicy` with policy name `"owns-task"`; `DeleteTodoTaskHandler` only needs to handle the now-narrower NotFound case since ownership is already settled by the time it runs. A non-owner's delete on an existing task returns 403 (denied before the handler runs - the task still exists afterward, proven via HTTP test); a delete on a nonexistent id returns 404 (policy lets it through as "no ownership violation to deny," handler's own NotFound fires) - the two distinct codes are the proof that policy enforcement is a separate, earlier pipeline stage.

**Divergence from the current `beginners-guide-todo-api.md` worth flagging for ticket 16**: that doc's existing Step 9 (`IRequiresPolicy`/coarse-only "is caller authenticated" check, not ownership-aware) predates this rebuild and hasn't been updated yet. This ticket's own acceptance criterion explicitly calls for ownership-based policy rejection, which is what's implemented. Ticket 16 (documentation sync) needs to rewrite Step 9's MS DI narrative to match this stronger design rather than porting the old coarse-check example as-is.

Unit tests (evaluator + handler) and persistence/HTTP tests (real SQLite + real host, proving pipeline-level enforcement) cover both paths. Full solution build (0 errors) and test (1158/1158) green.
