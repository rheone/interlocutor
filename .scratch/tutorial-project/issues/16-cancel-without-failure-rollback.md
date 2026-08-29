Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 8's "Bonus: forcing a rollback without a failure outcome" section - a `SyncAwareCompleteTodoTaskHandler`-style variant demonstrating `IUnitOfWork.Cancel`, using a fake/stand-in "external system already did this" dependency (the guide's own example uses an illustrative `ICalendarSyncClient`; invent a similarly small stand-in scoped to this project rather than a real external integration). Advanced-feature backlog item - demonstrates the rare case where a handler stages a write, then discovers mid-flight it shouldn't commit, without that being a `Failure`-shaped outcome.

## Acceptance criteria

- [x] A handler using `IUnitOfWork.Cancel` exists, exercised by a test proving the staged write was actually rolled back while the handler still returns success
- [x] A code comment explains why this is deliberately not the "real" `CompleteTodoTaskHandler` (ticket 08) - a rare-case tool, not a default every write handler needs

## Blocked by

12 (done - this ticket can start immediately)

## Comments

Promoted to `ready-for-agent` via `/to-tickets`, alongside 14-15, 17-18 - independent of each other, each already a single self-contained vertical slice.

Implemented: `Infrastructure/IExternalTaskSyncClient.cs` (the small stand-in), `Features/Tasks/SyncAwareCompleteTodoTask/{SyncAwareCompleteTodoTaskCommand,SyncAwareCompleteTodoTaskHandler}.cs`. This is a distinct command from `CompleteTodoTask.CompleteTodoTaskCommand` rather than a second handler for it - two closed handlers matching the same request/response pair is a compile-time diagnostic (`INTERLOC002`) with no `[HandlerOverride]` escape hatch, and the guide's own illustrative example makes the same point by never actually compiling alongside the real handler. Deliberately not wired into `Program.cs` (no endpoint) - matches the guide's framing as a rare-case tool, not the project's real completion path. Test coverage: `Unit/Features/Tasks/SyncAwareCompleteTodoTask/SyncAwareCompleteTodoTaskHandlerTests` (NSubstitute - Cancel vs. SaveChangesAsync called correctly) and `Persistence/SyncAwareCompleteTodoTaskRollbackTests` (real in-memory SQLite - proves the completion genuinely never reaches the database when cancelled, and genuinely does when not). `dotnet build`, `dotnet test` (967/967 solution-wide), `dotnet csharpier .` all clean.
