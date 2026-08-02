> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed types; see remediation 19.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Create formal Architecture Decision Records (ADRs) at `docs/adr/` derived from `bootstrapreference/ADR-mediator-result-pipeline.md`. Each ADR needs its own numbered file with status, context, decision, consequences, and alternatives.

Current bootstrap ADRs to include (from `ADR-mediator-result-pipeline.md`):
- 0001: C# 15 union types for Result
- 0002: Separate ValidationFailed vs RuleViolation
- 0003: Resource auth via template method
- 0004: Onion pipeline behaviors
- 0005: Exceptions reserved for bugs
- 0006: Single transactional scope
- 0007: CancellationToken at boundary only
- 0008: Marker interfaces for opt-in
- 0009: Lazy paired-marker session resolution
- 0010: Materialized collection results
- 0011: Validator composition unchanged

New ADRs needed based on decisions made during implementation:
- 0012: TransactionBehavior 2-param (not 3-param) — simplifies DI registration
- 0013: Forbidden returns StatusCodeResult(403) not ProblemDetails body — standard HTTP semantics
- 0014: Namespace split — Mediance.CommonKernel / Mediance.Abstractions

Update each ADR to have:
- Status: Accepted / Deprecated / Superseded
- Active cross-references to source code
- Correct package and type names
- Any consequences discovered during implementation

Also update `bootstrapreference/ADR-mediator-result-pipeline.md` to cross-reference the active ADRs.

## Acceptance criteria

- [ ] All 11 existing decisions have ADR files at `docs/adr/`
- [ ] 3 new ADRs added for implementation decisions
- [ ] Each ADR has Status, Context, Decision, Consequences, Alternatives
- [ ] Bootstrap ADR updated with reference to active ADRs
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
