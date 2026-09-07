# 01 - Tag and shelve the NHibernate/Autofac TutorialProject

**What to build:** The current `TutorialProject/TodoTasks.Api` (Autofac + NHibernate) and its tests are removed from the branch, but recoverably, not vanished. Before removal, tag the commit so the old project has a named anchor instead of a bare SHA. Write a resurrection reference alongside this ticket set that captures enough for someone to actually rebuild or republish the old example later: the tag name, the commit it points to, a short account of what it proved (a real, running-app exercise of the Autofac and NHibernate integration packages, not just their own unit tests), and a pointer back to the design conversation that decided this.

**Blocked by:** None - can start immediately

**Status:** done

- [x] The pre-removal commit is tagged with a clearly-named tag (e.g. `tutorial-project-nhibernate-autofac`)
- [x] `TutorialProject/TodoTasks.Api`, its test project, and their `Interlocutor.slnx` entries are removed from the branch
- [x] A resurrection reference document exists recording the tag, the commit SHA, what the old project proved, and why it was shelved
- [x] `dotnet build`/`dotnet test` are green solution-wide with the old project gone (nothing else referenced it)

## Comments

**2026-09-04** - Implemented: tagged `a872496` (the pre-removal HEAD) as `tutorial-project-nhibernate-autofac` (annotated tag). Removed `TutorialProject/TodoTasks.Api` and `tests/TodoTasks.Api.Tests` via `git rm -r`, and their two `Interlocutor.slnx` entries. Wrote `.scratch/tutorial-project-common-stack/resurrection-reference.md` recording the tag, commit, what the project proved, and why it was shelved. Full solution build (0 errors) and test (1065/1065, down from 1183 - the 118 TodoTasks.Api.Tests removed with the project) are green.
