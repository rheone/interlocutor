# 06 - Retire Interlocutor.EntityFrameworkCore package

**What to build:** The `Interlocutor.EntityFrameworkCore` package and its `TransactionBehavior` - superseded by ticket 02's behavior - are removed from the solution. Nothing in the shipped solution or its test suite references the retired package.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] `Interlocutor.EntityFrameworkCore` project and its test project removed from the solution
- [ ] No remaining reference to the retired package anywhere in `src/` or `samples/`
- [ ] Solution builds and the remaining test suite passes with the package gone
