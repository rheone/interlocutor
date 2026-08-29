> **Status: open**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/handler-selection-and-dispatch.md`, `documentation/builder-and-registration.md`, `documentation/dependency-injection-packages.md`, `documentation/dependency-injection-abstractions.md`, `documentation/dependency-injection-microsoft.md`, `documentation/dependency-injection-autofac.md` following the standing decisions in `../PRD.md`: remove stale language, verify against `src/Interlocutor.DependencyInjection.*` and `src/Interlocutor.Abstractions`, rebalance any outsized `OperationOutcome` emphasis, add glossary-first-mention links, add obsolete-alerts if any apply here. Consider whether the four `dependency-injection-*.md` files have redundant overlap worth consolidating (they cover: shared registration model, abstractions-specific detail, Microsoft-specific, Autofac-specific) - reorganize if it reduces duplication without losing container-specific detail.

## Acceptance criteria

- [ ] No stale language remains
- [ ] Every claim verified against current `src/` code
- [ ] Glossary-first-mention links added where appropriate
- [ ] Any reorganization keeps container-specific registration detail discoverable
