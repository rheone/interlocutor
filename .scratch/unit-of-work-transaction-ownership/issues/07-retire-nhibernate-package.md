# 07 - Retire Interlocutor.NHibernate package

**What to build:** The `Interlocutor.NHibernate` package and its `TransactionBehavior` - superseded by ticket 03's behavior - are removed from the solution. Nothing in the shipped solution or its test suite references the retired package.

**Blocked by:** 05.

**Status:** ready-for-agent

- [ ] `Interlocutor.NHibernate` project and its test project removed from the solution
- [ ] No remaining reference to the retired package anywhere in `src/` or `samples/`
- [ ] Solution builds and the remaining test suite passes with the package gone
