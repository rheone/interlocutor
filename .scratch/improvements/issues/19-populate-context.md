> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed types; see remediation 02 and 19.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Populate `CONTEXT.md` with domain vocabulary for the Mediance project. Currently it is a 5-line placeholder.

The domain glossary should cover:
- Mediance — what the name means and why it was chosen
- Mediator pattern — how Mediance implements it
- CQRS — query/command separation in Mediance
- Pipeline behavior — onion-model middleware
- Failure vocabulary — ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict
- Result type — discriminated union for success/failure
- Resource authorization — Load → Authorize → Handle pattern
- Transactional scope — ITransactionalRequest and lazy session resolution
- Marker interfaces — opt-in cross-cutting concerns
- PagedResult — materialized pagination type

See `bootstrapreference/Addendum-B-library-packaging.md` for the naming rationale. See `docs/agents/domain.md` for the expected format conventions.

## Acceptance criteria

- [ ] `CONTEXT.md` contains all domain vocabulary defined
- [ ] Terms match the actual source code (not bootstrap design)
- [ ] Format follows `docs/agents/domain.md` conventions
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
