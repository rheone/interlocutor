# 04 - EF Core DI registration

**What to build:** A consumer registers the new EF Core commit-gate behavior (ticket 02) through the DI builder the same way `AddTransactionBehavior()` works today, on both supported DI containers.

**Blocked by:** 02.

**Status:** ready-for-agent

- [ ] Microsoft DI package can register the new behavior for a given `DbContext` type
- [ ] Autofac package can register the new behavior
- [ ] Registration is opt-in only - a bare `AddInterlocutor()` call does not add it
- [ ] Registration follows the same try-add-dependencies convention documented for other pipeline behaviors
