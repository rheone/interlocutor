# Resurrection reference: the Autofac/NHibernate TutorialProject

The original `TutorialProject/TodoTasks.Api` - built on `Interlocutor.DependencyInjection.Autofac` +
`Interlocutor.Persistence.NHibernate` - was removed from this branch as part of the common-stack migration
(`.scratch/tutorial-project-common-stack/`). It isn't gone; it's shelved. This document is the pointer back to
it.

## Where it lives

- **Tag:** `tutorial-project-nhibernate-autofac`
- **Commit:** `a872496` (the last commit on `2026-09-04-ResearchTicketNormalization` before removal - `chore:
  complete vestigial-code-audit - sweep finding + cleanup`)

To get it back:

```bash
git checkout tutorial-project-nhibernate-autofac -- TutorialProject/TodoTasks.Api tests/TodoTasks.Api.Tests
```

or, to see the whole project tree as it stood, `git checkout tutorial-project-nhibernate-autofac` (detached
HEAD) and browse from there. The tag is an annotated tag, so `git show tutorial-project-nhibernate-autofac`
also gives the tag message and commit in one shot.

## What it proved

This was a full, runnable worked example of the [Beginner's Guide](../../documentation/beginners-guide-todo-api.md)
- not a toy, and not just unit tests of the Autofac/NHibernate integration packages in isolation. It exercised:

- `Interlocutor.DependencyInjection.Autofac`'s `AddInterlocutor()` registration, end to end, in a real ASP.NET
  Core host.
- `Interlocutor.Persistence.NHibernate`'s `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` implementation
  against a real NHibernate session (FluentNHibernate mapping, SQLite in-memory with the shared-cache workaround
  NHibernate's driver needed), including `UnitOfWorkBehavior`'s commit/rollback gate and the `Cancel()` escape
  hatch.
- The full vertical-slice feature set: Users (create/get/delete, including a multi-repository unit-of-work
  cascade delete) and TodoTasks (create, read, update, complete/reopen + notification, delete + policy
  authorization, clear-completed), plus a dev/test authentication handler, a QueryOver-based title search,
  paging, a second custom pipeline behavior, and a reordered-pipeline demo.
- That `documentation/beginners-guide-todo-api.md` and `documentation/sample-app-walkthrough.md`'s own
  narrative held up against a real compiling project, not just prose.

## Why it was shelved

Readers are more likely to already know Microsoft.Extensions.DependencyInjection and Entity Framework Core than
Autofac and Fluent NHibernate. Reaching for the less-common stack in the primary worked example asked more of a
first-time reader than the library itself does - `Interlocutor.DependencyInjection.Autofac` and
`Interlocutor.Persistence.NHibernate` are both fully-supported, equal-standing packages; the guide's original
choice to demo them was about matching what the Beginner's Guide happened to walk through first, not a
statement that they're the "main" stack. See `.scratch/tutorial-project-common-stack/PRD.md` for the full
decision record from the design conversation that decided this, including what carries over unchanged
(FluentValidation), what's out of scope for the rebuild, and the process notes for whoever resumes that effort.

The original build effort that produced this project in the first place - including the deliberate choice of
Autofac/NHibernate to match the Beginner's Guide's own stated stack at the time - is recorded in
`.scratch/tutorial-project/PRD.md`, which stays as the historical record of that decision.
