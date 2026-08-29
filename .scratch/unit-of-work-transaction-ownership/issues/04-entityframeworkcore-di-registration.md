# 04 - EF Core DI registration

**What to build:** A consumer registers the new EF Core commit-gate behavior (ticket 02) through the DI builder the same way `AddTransactionBehavior()` works today, on both supported DI containers.

**Blocked by:** 02.

**Status:** done

- [x] Microsoft DI package can register the new behavior for a given `DbContext` type
- [x] Autofac package can register the new behavior
- [x] Registration is opt-in only - a bare `AddInterlocutor()` call does not add it
- [x] Registration follows the same try-add-dependencies convention documented for other pipeline behaviors

## Comments

Already implemented in commit `c938568`, via a single container-neutral mechanism rather than a separate method per store per container: `InterlocutorPipelineBuilder.AddUnitOfWorkBehavior()` (`Interlocutor.DependencyInjection.Abstractions`) reflectively detects whether `Interlocutor.Persistence.EntityFrameworkCore` is referenced and registers its arity-3 `UnitOfWorkBehavior<,,>` if so - working identically on both Microsoft DI and Autofac, since both build on the same `InterlocutorPipelineBuilder`. Registration is opt-in (must call `.Pipeline.AddUnitOfWorkBehavior()`) and reports a `SkippedRegistration` when the package isn't referenced, matching the try-add-dependencies convention other behaviors use.
