Status: needs-triage

## Labels

ready-for-agent

## What to build

- New `documentation/persistence-abstraction.md` (or fold into an existing doc if a better home turns out to exist by the time this is picked up) - walkthrough of `IUnitOfWork`/`IDomainRepository<TEntity,TKey>`, following the house style conventions in `CLAUDE.md` (GitHub alert types, no enumerated counts in prose, inline mermaid only, truthful against shipped code). Cover: it's optional/opt-in, it's a façade over the same scoped session `TransactionBehavior` already manages (link to `transactions-and-store-integration.md` rather than re-explain), the CRUD/get-many/get-paged/escape-hatch surface, and how to register it per container.
- Coordinated edit to `CLAUDE.md`'s pagination sentence and `documentation/glossary.md`'s Pagination section: both currently read as "Interlocutor's core has no opinion about pagination," present tense. Add a clarifying note (in both places) that this remains true of `Interlocutor.CommonKernel` specifically, and that the optional `Interlocutor.Persistence.Abstractions` package's paging type is a different package's vocabulary for a different concern (repository return shape, not `OperationOutcome` success-case shape) - do not remove or contradict the existing sentence, clarify it.
- `README.md`'s package-layering section/diagram, if one exists by the time this is picked up, gets the three new packages added.

## Acceptance criteria

- [ ] New doc exists, every code example compiles against the shipped API (not aspirational)
- [ ] `CLAUDE.md` and `documentation/glossary.md` pagination sections are updated, not contradicted
- [ ] No internal design-record citations added to any `documentation/` file (per `CLAUDE.md`'s content rules) - state rationale as fact, don't cite ADR-0017 to justify a design choice inside `documentation/`
- [ ] Truthfulness check: every claim verified against the actually-shipped `src/Interlocutor.Persistence.*` code from issues 01-04, not carried forward from this PRD/ADR

## Blocked by

None hard - best done once the API from 01-04 is stable, but can start drafting earlier.
