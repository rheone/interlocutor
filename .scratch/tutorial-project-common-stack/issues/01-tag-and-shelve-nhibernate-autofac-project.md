# 01 - Tag and shelve the NHibernate/Autofac TutorialProject

**What to build:** The current `TutorialProject/TodoTasks.Api` (Autofac + NHibernate) and its tests are removed from the branch, but recoverably, not vanished. Before removal, tag the commit so the old project has a named anchor instead of a bare SHA. Write a resurrection reference alongside this ticket set that captures enough for someone to actually rebuild or republish the old example later: the tag name, the commit it points to, a short account of what it proved (a real, running-app exercise of the Autofac and NHibernate integration packages, not just their own unit tests), and a pointer back to the design conversation that decided this.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] The pre-removal commit is tagged with a clearly-named tag (e.g. `tutorial-project-nhibernate-autofac`)
- [ ] `TutorialProject/TodoTasks.Api`, its test project, and their `Interlocutor.slnx` entries are removed from the branch
- [ ] A resurrection reference document exists recording the tag, the commit SHA, what the old project proved, and why it was shelved
- [ ] `dotnet build`/`dotnet test` are green solution-wide with the old project gone (nothing else referenced it)
