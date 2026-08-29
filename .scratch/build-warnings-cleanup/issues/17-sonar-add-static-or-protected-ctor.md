# 17 - S1118: give `TodoTasksSessionFactory` a protected constructor or `static` keyword

**What to build:** SonarAnalyzer's S1118 fires on `TutorialProject/TodoTasks.Api/Program.cs(206,22)` for a class exposing only static members but lacking either a `static` class modifier or an explicit `protected` constructor to block instantiation. Confirm the flagged type is genuinely static-only (no instance state, no reason a caller would ever `new` it) and add the `static` keyword to its declaration - or, if it turns out to need instantiability for a reason not visible from the warning alone, add an explicit `protected` constructor instead and record why `static` doesn't fit.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero `S1118` warnings
- [ ] The fix (`static` keyword or `protected` constructor) matches the type's actual intended usage, not just whichever silences the warning fastest
- [ ] `dotnet test` passes for `TodoTasks.Api.Tests`
