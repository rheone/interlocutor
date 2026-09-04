# 08 - TodoTasks feature: Complete/Reopen + notification

**What to build:** Complete and Reopen endpoints/commands/handlers for TodoTasks, each publishing a notification on state change (matching the original project's multi-subscriber notification example).

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Completing an owned task transitions its state and publishes the notification
- [ ] Reopening does the same in reverse
- [ ] At least the same multi-subscriber shape as the original project's notification example is preserved
- [ ] Unit and persistence tests cover both transitions and the notification fan-out
