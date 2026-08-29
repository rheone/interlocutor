# 08 - Architecture tests

**What to build:** `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` enforces the shape this feature establishes: the retired packages are gone, and the new commit-gate behaviors live in `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` with the correct dependency direction.

**Blocked by:** 06, 07.

**Status:** done

- [x] Dependency rules asserting the old `Interlocutor.EntityFrameworkCore`/`.NHibernate` package shape are removed
- [x] New rules assert the commit-gate behaviors' package placement and dependency direction match ADR-0018
- [x] Full architecture test suite passes

## Comments

Already implemented in commit `c938568` - `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` contains `PersistenceEntityFrameworkCore_ReferencedAssembly_ContainsTheEfCoreCommitGateBehavior_Test` and `PersistenceNHibernate_ReferencedAssembly_ContainsTheNHibernateCommitGateBehavior_Test`, each asserting the commit-gate `UnitOfWorkBehavior` ships from the correct persistence package and citing ADR-0018 in its comment. No assertion anywhere in the file references the old `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` package shape. Full architecture test suite passes (12/12).
