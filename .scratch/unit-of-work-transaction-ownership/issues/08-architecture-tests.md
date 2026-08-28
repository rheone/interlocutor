# 08 - Architecture tests

**What to build:** `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` enforces the shape this feature establishes: the retired packages are gone, and the new commit-gate behaviors live in `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` with the correct dependency direction.

**Blocked by:** 06, 07.

**Status:** ready-for-agent

- [ ] Dependency rules asserting the old `Interlocutor.EntityFrameworkCore`/`.NHibernate` package shape are removed
- [ ] New rules assert the commit-gate behaviors' package placement and dependency direction match ADR-0018
- [ ] Full architecture test suite passes
