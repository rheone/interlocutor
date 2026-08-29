> **Status: done**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/handler-selection-and-dispatch.md`, `documentation/builder-and-registration.md`, `documentation/dependency-injection-packages.md`, `documentation/dependency-injection-abstractions.md`, `documentation/dependency-injection-microsoft.md`, `documentation/dependency-injection-autofac.md` following the standing decisions in `../PRD.md`: remove stale language, verify against `src/Interlocutor.DependencyInjection.*` and `src/Interlocutor.Abstractions`, rebalance any outsized `OperationOutcome` emphasis, add glossary-first-mention links, add obsolete-alerts if any apply here. Consider whether the four `dependency-injection-*.md` files have redundant overlap worth consolidating (they cover: shared registration model, abstractions-specific detail, Microsoft-specific, Autofac-specific) - reorganize if it reduces duplication without losing container-specific detail.

## Acceptance criteria

- [x] No stale language remains
- [x] Every claim verified against current `src/` code
- [x] Glossary-first-mention links added where appropriate
- [x] Any reorganization keeps container-specific registration detail discoverable

## Findings beyond the original scope

- `handler-selection-and-dispatch.md` used a nonexistent `[OverridesHandler]` attribute throughout - the real type is `HandlerOverrideAttribute` (`Interlocutor.Abstractions.Core`), used as `[HandlerOverride(...)]`. Worse, the example built around it modeled the wrong scenario entirely: the source's own diagnostics (`INTERLOC002` vs `INTERLOC006`) show the override escape hatch exists **only** for two open-generic handlers closing over the same concrete type - a closed-vs-closed duplicate (`INTERLOC002`) has no override and must be removed/renamed, and closed-vs-open was never ambiguous in the first place. Rewrote the flowchart and the two affected sections to match, including the correct default diagnostic severities (`INTERLOC001`/`INTERLOC002` are warnings by default, `INTERLOC006` is an error).
- `dependency-injection-packages.md` was a fourth, redundant file duplicating everything already covered - with less depth - by the `dependency-injection-abstractions.md` / `.microsoft.md` / `.autofac.md` trio (same package map, same registration-model tables, same comparison table, same generated-registrations section). `documentation/index.md`'s table of contents never linked to that trio at all - only to this redundant file - suggesting it's a leftover from before the split into three files. Deleted it and repointed the four files that referenced it (`README.md`, `index.md`, `persistence-implementations.md`, `sample-app-walkthrough.md`).
- `glossary.md`'s `RegistrationKind` entry was missing the `Dispatcher` case (the generator-emitted `IGeneratedRequestDispatcher` singleton) - added.
- `InterlocutorBuilder.RequireUniqueClosure`/`Eject`/`Eject<T>()` - a real, shipped extensibility mechanism for opting a consumer's own open-generic contracts into ambiguous-closure checking - had no documentation anywhere. Added a new section to `builder-and-registration.md` plus glossary entries.
