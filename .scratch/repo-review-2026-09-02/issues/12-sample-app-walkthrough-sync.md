# 12 - Sync `sample-app-walkthrough.md` with TutorialProject

**What to build:** The walkthrough's feature table and `Features/Tasks/` project-structure tree include `ListTodoTasksPaged`, `SearchTodoTasks`, and `SyncAwareCompleteTodoTask` alongside the eight slices already documented, so a reader following the doc as a guided tour actually learns these shipped, tested features exist.

Full technical detail: [findings/11-sample-app-walkthrough-stale.md](../findings/11-sample-app-walkthrough-stale.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The feature table lists all eleven Tasks use cases
- [ ] The `Features/Tasks/` project-structure tree includes all three missing slices
- [ ] A short section (mirroring the existing "Multi-repository unit-of-work example" treatment) covers `ListTodoTasksPaged` for `Page<T>` usage and `SyncAwareCompleteTodoTask` for `IUnitOfWork.Cancel`
