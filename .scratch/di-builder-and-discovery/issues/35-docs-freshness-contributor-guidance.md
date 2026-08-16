## Labels

ready-for-agent

## What to build

A short contributor-facing guidance note mapping each `documentation/*.md` file to the `src/*` package(s) it documents, so a contributor (human or agent) touching code knows which doc to check for staleness. This is guidance only, not enforcement - no CI check that blocks a PR for a missing doc update; that's easy to get wrong (false positives on changes that genuinely don't need a doc update) and isn't being asked for here.

Place it in the repo's existing contributor-facing instructions (`AGENTS.md`/`CLAUDE.md`) or, following the precedent already set by `docs/agents/` (which documents agent-facing process like the local issue-tracker convention and triage-label vocabulary), a new file alongside those - whichever location this repo's existing convention makes the more natural fit at the time this ticket is picked up.

The mapping itself: one line per `documentation/*.md` file produced by tickets 22–34, naming the `src/*` package(s) it covers.

## Acceptance criteria

- [ ] A mapping from each `documentation/*.md` file to the `src/*` package(s) it documents exists in contributor-facing guidance
- [ ] Guidance is advisory prose, not a CI/build-breaking check
- [ ] Placed consistently with this repo's existing convention for agent/contributor process docs, not a new parallel location

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
23 - Documentation: Pipeline & Behavior Composition
24 - Documentation: Handler Selection & Dispatch
25 - Documentation: Builder & Registration
26 - Documentation: IMediator/ISender/IPublisher, Lifetimes, Scope & Ambient Context
27 - Documentation: Messages, Result Types & Variance
28 - Documentation: Notifications & Fan-out
29 - Documentation: Runtime-Typed Dispatch
30 - Documentation: Transactions & Store Integration
31 - Documentation: ASP.NET Core Integration & HTTP Mapping
32 - Documentation: Testing Handlers & Pipelines
