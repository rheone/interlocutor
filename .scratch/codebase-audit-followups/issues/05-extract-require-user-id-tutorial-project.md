# 05 - Extract RequireUserId() for TutorialProject endpoints

**What to build:** Four TutorialProject endpoints (`CreateTodoTaskEndpoint`, `SearchTodoTasksEndpoint`, `UpdateTodoTaskEndpoint`, `ClearCompletedTodoTasksEndpoint`) each repeat the same "require an authenticated caller" guard:

```csharp
var ownerId = contextAccessor.Context?.UserId
    ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");
```

Extract this into a single `RequireUserId()` extension method on `IRequestContextAccessor` (or the concrete context type used in TutorialProject) and have all four endpoints call it.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `RequireUserId()` extension method exists with one definition of the guard and its exception message
- [ ] All four endpoints call the shared extension instead of inlining the guard
- [ ] TutorialProject remains on classic `this`-parameter extension method style (it is exempt from the C# 14 extension-block preference per CLAUDE.md)
- [ ] `tests/TodoTasks.Api.Tests` pass, including the unauthenticated-caller cases for all four endpoints
