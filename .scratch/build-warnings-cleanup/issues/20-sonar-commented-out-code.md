# 20 - S125: remove or justify commented-out code

**What to build:** SonarAnalyzer's S125 ("Remove this commented out code") fires on `src/Interlocutor.Abstractions/Mediator.cs(300,9)`, `tests/Interlocutor.DependencyInjection.Microsoft.Tests` (one occurrence), and `TutorialProject/TodoTasks.Api` (one occurrence). Read each flagged block: if it's genuinely dead code left over from an earlier approach, delete it (git history already preserves it if it's ever needed again); if it's a deliberate example/reference snippet inside a doc comment or explanatory comment that Sonar is mis-flagging as dead code, either rephrase it so it doesn't parse as commented-out code or suppress with a recorded reason.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged block is either deleted (confirmed dead, not a deliberate example) or kept with an inline explanation for why it's intentional
- [x] `dotnet build` reports zero `S125` warnings
- [x] `dotnet test` passes solution-wide

## Comments

All three flagged blocks were prose comments Sonar mis-flagged as commented-out code, not actual dead code - each ended in a semicolon-terminated phrase (`...that is always null;`, `...unwind in detail;`, `...is referenced);`) that parses like a truncated statement. Rephrased each to remove the code-like punctuation (`Mediator.cs`, `InterlocutorBuilderIntegrationTests.cs`, `TutorialProject/TodoTasks.Api/Program.cs`) without changing their meaning - no suppression needed.
