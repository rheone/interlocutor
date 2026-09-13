# 16 - Documentation sync

**What to build:** Bring the docs in line with the new project. `documentation/beginners-guide-todo-api.md` flips its primary walkthrough to MS DI/EF Core, with Autofac/NHibernate becoming the per-step callout-alternate (the guide already has this table/callout structure - invert it rather than rewriting from scratch). `documentation/sample-app-walkthrough.md` is rewritten to describe the new project's structure and stack. `CLAUDE.md`'s description of `TutorialProject/` (currently naming NHibernate/FluentValidation/Autofac) is corrected to name the new stack.

**Blocked by:** 06, 07, 08, 09, 10, 11, 12, 13, 14, 15

**Status:** done - implemented in `369b91c`. Most of this ticket (the primary/alternate stack flip, CLAUDE.md's description) was already satisfied by prior work; the genuine gap found was stale pre-Async dispatch member names (`Handle`/`Send`/`Publish`/`Cancel`) left over from before the separate async-method-naming rename removed those members from the dispatch interfaces entirely.

- [x] Every code example in `beginners-guide-todo-api.md` and `sample-app-walkthrough.md` compiles against the shipped API (house style rule for `documentation/`) - fixed: both files still used old `Handle`/`Send`/`Publish`/`Cancel` names; renamed to `HandleAsync`/`SendAsync`/`PublishAsync`/`CancelAsync` throughout, verified against `src/`
- [x] `beginners-guide-todo-api.md`'s stack table and per-step callouts read MS DI/EF Core as primary, Autofac/NHibernate as the alternate (already satisfied)
- [x] `CLAUDE.md`'s `TutorialProject/` description names the new stack (already satisfied)
- [x] No dangling references to the old project's specifics (NHibernate mapping files, Autofac container wiring) remain outside the callout sections (already satisfied)
