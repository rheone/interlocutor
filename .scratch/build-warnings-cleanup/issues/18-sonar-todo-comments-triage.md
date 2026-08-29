# 18 - S1135: triage every `TODO` comment SonarAnalyzer now flags

**What to build:** SonarAnalyzer's S1135 ("Complete the task associated to this 'TODO' comment") fires on every `// TODO` in the solution - roughly forty in `TutorialProject/TodoTasks.Api` and seventeen in `tests/TodoTasks.Api.Tests` (re-run `dotnet build -t:Rebuild` after `SonarAnalyzer.CSharp` lands, per ticket 15, to get the current, authoritative list and line numbers). Triage each one individually: resolve and remove the comment if the referenced work is actually small and in scope; convert it into a tracked `.scratch/` ticket (this repo's issue tracker) and reference that ticket from the comment if it's real, non-trivial follow-up work; or, if a `TODO` no longer reflects a real gap (the referenced work already happened, or the concern turned out not to apply), remove it as stale. Don't mass-suppress the rule - each comment needs an actual decision.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every `TODO` comment in the solution has been read and triaged (resolved-and-removed, converted to a tracked ticket with a reference, or removed as stale) - none are left as a bare, unactioned `TODO`
- [ ] Every new ticket filed as part of this triage lives under `.scratch/<feature>/issues/` per this repo's issue-tracker convention, not as a comment-only note
- [ ] `dotnet build` reports zero `S1135` warnings
- [ ] `dotnet test` passes solution-wide
