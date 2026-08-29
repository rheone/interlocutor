# 23 - S2139: LoggingBehavior's caught exception is neither logged nor rethrown with context

**What to build:** SonarAnalyzer's S2139 ("Either log this exception and handle it, or rethrow it with some contextual information") fires on `src/Interlocutor.Abstractions/Pipeline/LoggingBehavior.cs(37,26)`. Read the surrounding `catch` block: if it's a legitimate "log and continue" or "log and rethrow" pattern that Sonar is only flagging because the logging call doesn't include the caught exception itself (a common false-flag shape), pass the exception into the log call (or its own dedicated overload) so it's captured; if the catch is doing something else entirely (e.g. deliberately swallowing to preserve an outcome-based failure contract rather than throwing), suppress with a comment explaining why swallowing without re-logging is correct here.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The flagged `catch` block either logs the caught exception with full context or rethrows it (preserving the original stack trace via a bare `throw;`, not `throw ex;`) - or carries a recorded reason for why neither applies
- [ ] `dotnet build` reports zero `S2139` warnings
- [ ] `dotnet test` passes for `Interlocutor.Abstractions.Tests`
