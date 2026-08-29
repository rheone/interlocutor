# 09 - Samples update

**What to build:** The runnable sample app demonstrates a write going through the new unit-of-work-owned transaction model end to end, with no reference to the retired `TransactionBehavior` packages. The bootstrap reference doc's code is updated to match.

**Blocked by:** 04, 05.

**Status:** done

- [x] `samples/Interlocutor.Sample.WebApi` builds and runs against the new model
- [x] `docs/bootstrapreference/TransactionBehavior.cs` reference sample updated or replaced to match the new model
- [x] Sample is verified against a real `ServiceProvider`, per the project's existing convention for this sample

## Comments

This ticket's premise is stale: `samples/Interlocutor.Sample.WebApi` no longer exists - it was removed entirely in a separate commit ("chore: remove outdated samples/ projects"), superseded by `TutorialProject/TodoTasks.Api` as this repo's runnable worked example (see CLAUDE.md). That project already demonstrates a write going through the new unit-of-work-owned transaction model end to end with no reference to the retired `TransactionBehavior` packages (`AddInterlocutorUnitOfWork`, `NHibernateUnitOfWork`, and NHibernate's `UnitOfWorkBehavior` via `AddUnitOfWorkBehavior()`, exercised by `tests/TodoTasks.Api.Tests`), and is verified against a real container (Autofac) rather than a bare `ServiceProvider`, matching this project's actual DI choice. `maintainer_docs/bootstrapreference/UnitOfWorkBehavior.cs` (the renamed successor to a `TransactionBehavior.cs` reference file) already documents the new model. Marking done on that basis rather than reviving a retired sample project.
