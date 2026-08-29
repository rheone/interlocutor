> **Status: open**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/aspnetcore-integration.md`, `documentation/transactions-and-store-integration.md`, `documentation/persistence-abstraction.md`, `documentation/persistence-implementations.md` following the standing decisions in `../PRD.md`. `aspnetcore-integration.md` remains the only file permitted to mention HTTP status codes - confirm the other three don't. `transactions-and-store-integration.md` already correctly states `ITransactionalRequest` "no longer means what it used to" as a factual clarification of current behavior, not stale language to remove - keep that framing but re-check the wording reads as current-state, not history. Verify persistence docs against `src/Interlocutor.Persistence.Abstractions`, `.EntityFrameworkCore`, `.NHibernate`.

## Acceptance criteria

- [ ] HTTP status codes appear only in `aspnetcore-integration.md`
- [ ] No stale language remains
- [ ] Every claim verified against current `src/` persistence code
- [ ] Glossary-first-mention links added where appropriate
