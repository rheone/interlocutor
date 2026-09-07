# TutorialProject: Common-Stack Migration (MS DI + EF Core)

Status: **not started**. Tracked as issues under `.scratch/tutorial-project-common-stack/issues/`.

This supersedes `TutorialProject/TodoTasks.Api`'s current stack (Autofac + NHibernate) with the conventional Microsoft stack (`Microsoft.Extensions.DependencyInjection` + Entity Framework Core), on the reasoning that readers are more likely to already know MS DI and EF Core than Autofac and Fluent NHibernate. The original build effort - including the deliberate choice of Autofac/NHibernate to match [the Beginner's Guide](../../documentation/beginners-guide-todo-api.md)'s own stated stack - is recorded in `.scratch/tutorial-project/PRD.md` and stays there as the historical record; this file does not duplicate it, only the decisions that differ.

## Original request (paraphrased from a design conversation)

> The example TodoTasks.Api project uses Autofac and NHibernate, which are less common than MS DI and Entity Framework that readers are likely to be more familiar with. We shouldn't reach for uncommon libraries in the example project. Rewrite with "common" conventions, and update the walkthrough doc in tandem.

## Decisions from the grilling session

- **Scope: full swap in place**, not a second parallel project and not a docs-only relabeling. `TutorialProject/TodoTasks.Api` gets rebuilt on `Interlocutor.DependencyInjection.Microsoft` + `Interlocutor.Persistence.EntityFrameworkCore`. FluentValidation is unchanged - it was never the "uncommon" part of the stack.
- **Persistence**: EF Core via `Microsoft.EntityFrameworkCore.Sqlite`, in-memory (`Data Source=:memory:` on one kept-open connection). This preserves the original project's "real relational engine, no external dependency" property without NHibernate's driver limitations that forced a shared-cache workaround last time - EF Core's SQLite provider supports in-memory mode natively. Considered and rejected: `Microsoft.EntityFrameworkCore.InMemory` - Microsoft's own docs flag it as not relational-behavior-accurate (no real SQL, no constraint enforcement), which weakens it as a realistic demo.
- **Feature parity**: every feature from the original build - Users (create/get/delete, including the multi-repository unit-of-work cascade delete), TodoTasks (create, read, update, complete/reopen + notification, delete + policy authorization, clear-completed), the dev/test authentication handler, paging, a second custom pipeline behavior, and the reordered-pipeline demo - gets rebuilt on the new stack. Two items from the original's advanced-feature backlog are **out of scope** for this effort by the user's own approval of the ticket breakdown: the manual-response-mapping/MVC-controller variant, and the `Cancel`-without-failure rollback demo.
- **NHibernate-only feature has no literal port**: the original's QueryOver-based title search (ticket 14 in `.scratch/tutorial-project/`) doesn't have an NHibernate-specific equivalent under EF Core - it gets rebuilt as an EF Core LINQ/`EF.Functions`-based case-insensitive search instead, dropping to `DbContext` directly where `IDomainRepository<TEntity, TKey>`'s fixed method set doesn't reach, matching the original's own rationale for bypassing the repository.
- **The old project is shelved, not relocated in-tree.** An unbuilt, excluded-from-CI copy sitting in the tree would rot quietly through future refactors with nothing to catch its drift. Instead: delete it from the branch, tag the pre-deletion commit as a named anchor, and write a resurrection reference capturing enough to rebuild or republish it later - not just a bare SHA someone has to hunt for. Git history is the shelf.
- **This deliberately departs from `.scratch/tutorial-project/PRD.md`'s own process note** ("implementation happens in its own git worktree/branch, never on a docs branch"). This effort's tickets live on whatever branch is current when they're worked - confirmed during grilling that no other branch or worktree has unmerged work in flight, so there's nothing for a shared branch to outpace. A future reader comparing the two PRDs' process notes should read this as an intentional, one-off deviation, not an inconsistency.
- **Docs updated in the same effort, not deferred**: `documentation/beginners-guide-todo-api.md` flips its primary walkthrough to MS DI/EF Core (Autofac/NHibernate becomes the per-step callout-alternate - the guide already has this table/callout structure, just inverted), `documentation/sample-app-walkthrough.md` is rewritten for the new project, and `CLAUDE.md`'s description of `TutorialProject/` (currently "NHibernate/FluentValidation/Autofac") is corrected to match.

## Ticket checklist

- [ ] 01 - Tag and shelve the NHibernate/Autofac TutorialProject
- [ ] 02 - Project & test-harness scaffolding (MS DI + EF Core)
- [ ] 03 - Users feature: Create & Get
- [ ] 04 - Dev/test authentication handler
- [ ] 05 - TodoTasks feature: Create
- [ ] 06 - TodoTasks feature: Read (GetById + List)
- [ ] 07 - TodoTasks feature: Update
- [ ] 08 - TodoTasks feature: Complete/Reopen + notification
- [ ] 09 - TodoTasks feature: Delete + policy authorization
- [ ] 10 - TodoTasks feature: ClearCompleted
- [ ] 11 - TodoTasks feature: Search (EF Core equivalent)
- [ ] 12 - TodoTasks feature: Paging
- [ ] 13 - Users feature: Delete (multi-repo cascade)
- [ ] 14 - Custom pipeline behavior demo (slow-handler warning)
- [ ] 15 - Reordered pipeline demo (Validation before Authorization)
- [ ] 16 - Documentation sync

## Process notes for whoever resumes this effort

- 01 → 02 → 03 → 04 → 05 is a strict linear chain. Once 05 lands, 06/07/08/09/11/12/13/14/15 fan out in parallel (12 also needs 06; 10 also needs 08). 16 waits on everything.
- After each ticket: `dotnet build`, that ticket's tests, `dotnet csharpier .`, a Conventional Commit, check the box here and in the ticket's own file, append a short `## Comments` note recording what actually happened - matches this repo's existing ticket convention (see `.scratch/tutorial-project/issues/*.md` for the pattern).
- Run the full `dotnet test` suite (not just the new project) before considering the effort done.
