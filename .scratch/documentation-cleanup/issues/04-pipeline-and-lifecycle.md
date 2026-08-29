> **Status: open**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/pipeline-and-behaviors.md`, `documentation/mediator-lifetimes-and-scope.md`, `documentation/notifications-and-fan-out.md`, `documentation/runtime-typed-dispatch.md` following the standing decisions in `../PRD.md`. `pipeline-and-behaviors.md` currently gives `IHasTimeout`/`IRateLimited`/`ICancellable` only passing mentions compared to the deeper treatment other markers get elsewhere - consider whether each deserves a short dedicated subsection here, and add the `[Obsolete]` warning alerts for `Cancelled`/`RateLimited`/`Timeout` if this is a better home for them than unit 02's `messages-and-result-types.md` (pick one canonical home, cross-link from the other file instead of duplicating the alert).

## Acceptance criteria

- [ ] No stale language remains
- [ ] Every claim verified against current `src/Interlocutor.Abstractions` pipeline/marker types
- [ ] `IHasTimeout`/`IRateLimited`/`ICancellable` get proportionate coverage
- [ ] Glossary-first-mention links added where appropriate
