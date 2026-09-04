# 09 - TodoTasks feature: Delete + policy authorization

**What to build:** Delete endpoint/command/handler for TodoTasks, gated by `IRequiresPolicy`-based authorization (matching the original project's use of `AuthorizationBehavior`).

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Deleting an owned task succeeds; deleting a non-owned task is rejected by the authorization policy before the handler runs
- [ ] Unit and persistence tests cover both paths
