# 05 - NHibernate DI registration

**What to build:** A consumer registers the new NHibernate commit-gate behavior (ticket 03) through the DI builder the same way `AddTransactionBehavior()` works today, on both supported DI containers.

**Blocked by:** 03.

**Status:** ready-for-agent

- [ ] Microsoft DI package can register the new behavior
- [ ] Autofac package can register the new behavior
- [ ] Registration is opt-in only - a bare `AddInterlocutor()` call does not add it
- [ ] Registration follows the same try-add-dependencies convention documented for other pipeline behaviors
