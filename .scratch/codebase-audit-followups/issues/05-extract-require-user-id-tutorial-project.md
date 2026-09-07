# 05 - Extract RequireUserId() for TutorialProject endpoints

**What to build:** Four TutorialProject endpoints (`CreateTodoTaskEndpoint`, `SearchTodoTasksEndpoint`, `UpdateTodoTaskEndpoint`, `ClearCompletedTodoTasksEndpoint`) each repeat the same "require an authenticated caller" guard:

```csharp
var ownerId = contextAccessor.Context?.UserId
    ?? throw new InvalidOperationException("This endpoint requires an authenticated caller.");
```

Extract this into a single `RequireUserId()` extension method on `IRequestContextAccessor` (or the concrete context type used in TutorialProject) and have all four endpoints call it.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `RequireUserId()` extension method exists with one definition of the guard and its exception message
- [x] All four endpoints call the shared extension instead of inlining the guard
- [x] TutorialProject remains on classic `this`-parameter extension method style (it is exempt from the C# 14 extension-block preference per CLAUDE.md)
- [x] `tests/TodoTasks.Api.Tests` pass, including the unauthenticated-caller cases for all four endpoints

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `RequireUserId()` exists on `RequestContextAccessorExtensions` and all four named endpoints call it. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
