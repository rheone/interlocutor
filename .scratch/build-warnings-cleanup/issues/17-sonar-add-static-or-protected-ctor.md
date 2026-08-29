# 17 - S1118: give `TodoTasksSessionFactory` a protected constructor or `static` keyword

**What to build:** SonarAnalyzer's S1118 fires on `TutorialProject/TodoTasks.Api/Program.cs(206,22)` for a class exposing only static members but lacking either a `static` class modifier or an explicit `protected` constructor to block instantiation. Confirm the flagged type is genuinely static-only (no instance state, no reason a caller would ever `new` it) and add the `static` keyword to its declaration - or, if it turns out to need instantiability for a reason not visible from the warning alone, add an explicit `protected` constructor instead and record why `static` doesn't fit.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `dotnet build` reports zero `S1118` warnings
- [x] The fix (`static` keyword or `protected` constructor) matches the type's actual intended usage, not just whichever silences the warning fastest
- [x] `dotnet test` passes for `TodoTasks.Api.Tests`

## Comments

The flagged type is the top-level-statements `Program` marker class in `TutorialProject/TodoTasks.Api/Program.cs`, not `TodoTasksSessionFactory` as this ticket's title suggested - the title was wrong, but the ticket body's own file/line reference (`Program.cs(206,22)`) was accurate throughout, and that's what got fixed. `static` doesn't fit: `WebApplicationFactory<Program>` requires its type argument to be an instantiable class. Added an explicit `protected` constructor instead, with a `<remarks>` explaining why `static` was rejected.
