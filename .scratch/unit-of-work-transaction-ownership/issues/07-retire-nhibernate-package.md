# 07 - Retire Interlocutor.NHibernate package

**What to build:** The `Interlocutor.NHibernate` package and its `TransactionBehavior` - superseded by ticket 03's behavior - are removed from the solution. Nothing in the shipped solution or its test suite references the retired package.

**Blocked by:** 05.

**Status:** done

- [x] `Interlocutor.NHibernate` project and its test project removed from the solution
- [x] No remaining reference to the retired package anywhere in `src/` or `samples/`
- [x] Solution builds and the remaining test suite passes with the package gone

## Comments

Already implemented in commit `c938568`, same treatment as ticket 06 - `Interlocutor.NHibernate` and its test project were deleted; no remaining reference anywhere in `src/`.
