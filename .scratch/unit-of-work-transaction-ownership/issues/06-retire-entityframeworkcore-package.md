# 06 - Retire Interlocutor.EntityFrameworkCore package

**What to build:** The `Interlocutor.EntityFrameworkCore` package and its `TransactionBehavior` - superseded by ticket 02's behavior - are removed from the solution. Nothing in the shipped solution or its test suite references the retired package.

**Blocked by:** 04.

**Status:** done

- [x] `Interlocutor.EntityFrameworkCore` project and its test project removed from the solution
- [x] No remaining reference to the retired package anywhere in `src/` or `samples/`
- [x] Solution builds and the remaining test suite passes with the package gone

## Comments

Already implemented in commit `c938568` - `Interlocutor.EntityFrameworkCore` and its test project were deleted (`git show c938568 --stat` shows the removal). Verified against the current tree: no `src/Interlocutor.EntityFrameworkCore` directory exists, and a solution-wide grep for `Interlocutor.EntityFrameworkCore` (excluding `.scratch/`/`maintainer_docs/` historical records) finds nothing in `src/` or the (since-removed) `samples/`. Full solution builds and tests green.
