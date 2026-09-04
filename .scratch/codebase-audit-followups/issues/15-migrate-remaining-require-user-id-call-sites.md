# 15 - Migrate remaining inlined auth-guard call sites to RequireUserId()

**What to build:** Ticket 05 extracted `IRequestContextAccessor.RequireUserId()` and migrated the four endpoints it named (`CreateTodoTaskEndpoint`, `SearchTodoTasksEndpoint`, `UpdateTodoTaskEndpoint`, `ClearCompletedTodoTasksEndpoint`), but the same inlined guard also exists in `ListTodoTasksPagedEndpoint.cs` and three places in `TodoTasksController.cs` - discovered during ticket 05's implementation, out of its scope. Migrate these remaining call sites onto the same `RequireUserId()` extension so the guard has exactly one implementation across TutorialProject.

**Blocked by:** None - can start immediately (ticket 05 already merged `RequireUserId()`)

**Status:** ready-for-agent

- [ ] `ListTodoTasksPagedEndpoint.cs` calls `RequireUserId()` instead of inlining the guard
- [ ] All three inlined guard occurrences in `TodoTasksController.cs` call `RequireUserId()` instead
- [ ] No remaining inlined `contextAccessor.Context?.UserId ?? throw ...` guard exists anywhere in TutorialProject
- [ ] `tests/TodoTasks.Api.Tests` pass, including the unauthenticated-caller cases for the newly-migrated endpoints/actions
