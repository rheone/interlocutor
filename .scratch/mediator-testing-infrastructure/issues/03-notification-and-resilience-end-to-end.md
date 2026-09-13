# 03 — Interlocutor.IntegrationTests: notification fan-out and resilience behaviors, end-to-end

**What to build:** Extend the real host built in ticket 02 to prove that commands carrying resilience marker interfaces and cross-cutting notifications behave correctly together in practice — not just in each package's own isolated tests. This is the scenario coverage that per-package integration tests can't provide, because it depends on multiple behaviors and the real notification pipeline interacting in the same request.

**Blocked by:** 02 (needs the real host/DI scaffold)

**Status:** ready-for-agent

- [ ] A real command implementing both `ITransactionalRequest` and `IRateLimitedRequest` (or `ITimeoutRequest`) proves the resilience behavior and the transaction behavior interact correctly — e.g. a rate-limited or timed-out request never reaches the transaction commit
- [ ] A real notification published from within a command handler reaches multiple real `INotificationHandler` implementations, in the documented fan-out order, through the real `IPublisher`
- [ ] Both scenarios run in the same host/DI container built in ticket 02, not a newly constructed one
