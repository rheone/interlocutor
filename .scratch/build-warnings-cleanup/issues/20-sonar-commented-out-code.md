# 20 - S125: remove or justify commented-out code

**What to build:** SonarAnalyzer's S125 ("Remove this commented out code") fires on `src/Interlocutor.Abstractions/Mediator.cs(300,9)`, `tests/Interlocutor.DependencyInjection.Microsoft.Tests` (one occurrence), and `TutorialProject/TodoTasks.Api` (one occurrence). Read each flagged block: if it's genuinely dead code left over from an earlier approach, delete it (git history already preserves it if it's ever needed again); if it's a deliberate example/reference snippet inside a doc comment or explanatory comment that Sonar is mis-flagging as dead code, either rephrase it so it doesn't parse as commented-out code or suppress with a recorded reason.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every flagged block is either deleted (confirmed dead, not a deliberate example) or kept with an inline explanation for why it's intentional
- [ ] `dotnet build` reports zero `S125` warnings
- [ ] `dotnet test` passes solution-wide
