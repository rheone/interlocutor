Status: backlog - deferred, do not start until explicitly promoted to ready-for-agent

## Labels

needs-triage

## What to build

`documentation/beginners-guide-todo-api.md` Step 8's "Bonus: forcing a rollback without a failure outcome" section - a `SyncAwareCompleteTodoTaskHandler`-style variant demonstrating `IUnitOfWork.Cancel`, using a fake/stand-in "external system already did this" dependency (the guide's own example uses an illustrative `ICalendarSyncClient`; invent a similarly small stand-in scoped to this project rather than a real external integration). Advanced-feature backlog item - demonstrates the rare case where a handler stages a write, then discovers mid-flight it shouldn't commit, without that being a `Failure`-shaped outcome.

## Acceptance criteria

- [ ] A handler using `IUnitOfWork.Cancel` exists, exercised by a test proving the staged write was actually rolled back while the handler still returns success
- [ ] A code comment explains why this is deliberately not the "real" `CompleteTodoTaskHandler` (ticket 08) - a rare-case tool, not a default every write handler needs

## Blocked by

12
