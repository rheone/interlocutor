# 05 - NHibernate DI registration

**What to build:** A consumer registers the new NHibernate commit-gate behavior (ticket 03) through the DI builder the same way `AddTransactionBehavior()` works today, on both supported DI containers.

**Blocked by:** 03.

**Status:** done

- [x] Microsoft DI package can register the new behavior
- [x] Autofac package can register the new behavior
- [x] Registration is opt-in only - a bare `AddInterlocutor()` call does not add it
- [x] Registration follows the same try-add-dependencies convention documented for other pipeline behaviors

## Comments

Already implemented in commit `c938568`, via the same shared `InterlocutorPipelineBuilder.AddUnitOfWorkBehavior()` mechanism as ticket 04 - it probes for `Interlocutor.Persistence.NHibernate`'s arity-2 `UnitOfWorkBehavior<,>` when EF Core isn't referenced, on both DI containers, opt-in only, with the same `SkippedRegistration` reporting.
