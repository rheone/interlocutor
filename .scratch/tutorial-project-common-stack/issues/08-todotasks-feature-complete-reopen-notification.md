# 08 - TodoTasks feature: Complete/Reopen + notification

**What to build:** Complete and Reopen endpoints/commands/handlers for TodoTasks, each publishing a notification on state change (matching the original project's multi-subscriber notification example).

**Blocked by:** 05

**Status:** done

- [x] Completing an owned task transitions its state and publishes the notification
- [x] Reopening does the same in reverse
- [x] At least the same multi-subscriber shape as the original project's notification example is preserved
- [x] Unit and persistence tests cover both transitions and the notification fan-out

## Comments

**2026-09-04** - Implemented: `CompleteTodoTaskHandler` transitions `IsCompleted` and publishes `TodoTaskCompletedNotification`, fanning out to `TodoTaskCompletionCounterHandler` (`ICompletionCounter`) and `TodoTaskCompletionAuditLogHandler` (`ILogger`) - the same multi-subscriber pair the shelved project demonstrated. `ReopenTodoTaskHandler` reverses the transition and deliberately does not publish (matches the shelved project and the guide's "mirror image, no notification" framing). Both are owner-scoped, hand-written rather than `ResourceCommandHandler`-based - that base class is fixed to `Outcome<TSuccess, NotFound, Forbidden>` with no room for the domain-invalid-transition `Conflict` case Complete/Reopen also need, so `Outcome<TodoTaskDto, NotFound, Forbidden, Conflict>` is hand-rolled instead. Fan-out is proven at three layers: each subscriber unit-tested in isolation, the handler proven to call `IPublisher.Publish` once, and an HTTP test proving both concrete subscribers actually ran from one real publish through the real DI-resolved `IPublisher`. Full solution build (0 errors) and test (1145/1145) green.
