# 12 - Sync `sample-app-walkthrough.md` with TutorialProject

**What to build:** The walkthrough's feature table and `Features/Tasks/` project-structure tree include `ListTodoTasksPaged`, `SearchTodoTasks`, and `SyncAwareCompleteTodoTask` alongside the eight slices already documented, so a reader following the doc as a guided tour actually learns these shipped, tested features exist.

Full technical detail: [findings/11-sample-app-walkthrough-stale.md](../findings/11-sample-app-walkthrough-stale.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The feature table lists all eleven Tasks use cases
- [x] The `Features/Tasks/` project-structure tree includes all three missing slices
- [x] A short section (mirroring the existing "Multi-repository unit-of-work example" treatment) covers `ListTodoTasksPaged` for `Page<T>` usage and `SyncAwareCompleteTodoTask` for `IUnitOfWork.Cancel`

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. documentation/sample-app-walkthrough.md's feature table and Features/Tasks/ tree now include ListTodoTasksPaged, SearchTodoTasks, and SyncAwareCompleteTodoTask, each with its own section. No source changes were needed this session.
