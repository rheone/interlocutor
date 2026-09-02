Status: needs-triage
Severity: MEDIUM
Category: documentation / convention
Source: tests-conventions.md lane, confirmed by antagonist lane

# `documentation/sample-app-walkthrough.md` is stale — three shipped feature slices are undocumented

## Summary

The doc's feature table and `Features/Tasks/` project-structure tree list only the eight base Tasks use cases. The actual `TutorialProject` also ships `ListTodoTasksPaged`, `SearchTodoTasks`, and `SyncAwareCompleteTodoTask` (tickets 14-16 in `.scratch/tutorial-project/issues/`, all done), none of which appear anywhere in the walkthrough.

## Evidence

`TutorialProject/TodoTasks.Api/Features/Tasks/` contains `ListTodoTasksPaged/`, `SearchTodoTasks/`, and `SyncAwareCompleteTodoTask/` alongside the eight the doc lists — all real, wired-up (`Program.cs` maps the first two; the third is deliberately not, per its own ticket) vertical slices with their own test coverage. `.scratch/tutorial-project/issues/12-documentation-sync.md` (the ticket that last synced this doc) precedes tickets 14-18 that added these slices, and no later ticket updated `sample-app-walkthrough.md` — this violates CLAUDE.md's own cited policy (`maintainer_docs/agents/documentation-maintenance.md`) that docs are updated in the same iteration as the code change that makes them stale.

A reader using this doc as "a guided tour of the runnable project" (its own opening line) will not learn these three slices exist — including the `Page<T>` paging demonstration and the `IUnitOfWork.Cancel` rollback-without-failure demonstration, both directly relevant to sections the doc already covers.

## Suggested action

Add the three missing slices to the feature table and the `Features/Tasks/` tree. Consider a short section (mirroring the existing "Multi-repository unit-of-work example" treatment) pointing at `ListTodoTasksPaged` for `Page<T>` usage and `SyncAwareCompleteTodoTask` for `IUnitOfWork.Cancel`, since both are genuinely-shipped, documented-elsewhere (`beginners-guide-todo-api.md`) features this walkthrough currently omits entirely.
