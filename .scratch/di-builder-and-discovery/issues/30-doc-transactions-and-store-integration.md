> **Amendment** - this ticket instructed citing ADR-0011 and `docs/PRD.md` §9.2 directly in the shipped `documentation/transactions-and-store-integration.md` prose. That instruction is superseded: `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths - see the house convention added in ticket 36. The shipped file has been corrected by ticket 36. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

A `documentation/transactions-and-store-integration.md` covering `Interlocutor.EntityFrameworkCore` and `Interlocutor.NHibernate` - the current README's "Database Transactions" section has no home in the rest of this ticket batch, and it's store-specific enough to deserve its own doc rather than folding into the general pipeline doc (ticket 23). Primary sources: `docs/PRD.md` §9.2 (transaction behavior, lazy session resolution) and the marker-interface descriptions already migrated into `documentation/glossary.md` by ticket 21. The bootstrap PRD's §6.4/Addendum A.1 has useful historical rationale for the lazy-resolution requirement but is not itself authoritative - verify against current `src/Interlocutor.EntityFrameworkCore` and `src/Interlocutor.NHibernate` source.

Cover:

- **`TransactionBehavior`** for EF Core (`DbContext`) and NHibernate (`ISession`) - what each does and how a consumer opts a command into it.
- **`ITransactionalRequest` / `IExternalStoreRequest`** markers: the explicit, compiler-visible choice between "this command needs the SQL unit-of-work" and "this command is backed by something else" - and why that choice is explicit rather than inferred from absence.
- **Lazy session resolution**: `TransactionBehavior` never even constructs the store session for a non-transactional request, not just "doesn't wrap it in a transaction" - flagged as `[!IMPORTANT]` since some drivers open connections eagerly on construction, so getting this wrong has a real cost.
- **Single scoped session per dispatch**: the guarantee that a lookup and a write inside one command execute against the same session, backed by DI scoped lifetime (link to ticket 26 for the mediator's own scoped-lifetime story rather than re-explaining it).
- **Commit/rollback semantics**: commit happens once, only when the result is not a `Failure` case - any failure outcome (not just a thrown exception) rolls back.

## Acceptance criteria

- [ ] Both `TransactionBehavior` implementations (EF Core, NHibernate) are documented
- [ ] `ITransactionalRequest`/`IExternalStoreRequest` markers are documented with the rationale for explicit opt-in over inference
- [ ] Lazy session resolution is documented and flagged `[!IMPORTANT]` with the reason it matters
- [ ] Single-scoped-session-per-dispatch guarantee is documented, linking to ticket 26 rather than duplicating its scope explanation
- [ ] Commit/rollback-on-Failure semantics are documented
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
